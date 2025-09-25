using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.Queries;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.ReviewerSuggestion;
using App.Entities.Entities.App;
using App.Entities.Entities.Core; 
using App.Entities.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Text;
using System.Globalization;
using App.BLL.Services; // For GeminiAIService and VectorMath
using Microsoft.Extensions.Logging;

namespace App.BLL.Implementations
{
    public class ReviewerSuggestionService : IReviewerSuggestionService
    {
        private readonly GeminiAIService _aiService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReviewerSuggestionService> _logger;
        // Simple in-memory cache for reviewer TF maps and norms
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, (Dictionary<string, decimal> Tf, decimal Norm, DateTime ExpiresAt)> _reviewerTfCache = new();
        private static readonly TimeSpan ReviewerTfCacheTtl = TimeSpan.FromMinutes(30);
    // Cache for reviewer embeddings (semantic vectors) to avoid repeated external calls
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, (float[] Emb, DateTime ExpiresAt)> _reviewerEmbeddingCache = new();
    private static readonly TimeSpan ReviewerEmbeddingCacheTtl = TimeSpan.FromHours(1);

        public ReviewerSuggestionService(GeminiAIService aiService, IUnitOfWork unitOfWork, ILogger<ReviewerSuggestionService> logger)
        {
            _aiService = aiService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<BaseResponseModel<ReviewerSuggestionOutputDTO>> SuggestReviewersBySubmissionIdAsync(ReviewerSuggestionBySubmissionInputDTO input)
        {
            try
            {
                // Step 1: Fetch the submission and related entities
                var submission = await _unitOfWork.GetRepo<Submission>().GetSingleAsync(
                    new QueryOptions<Submission>
                    {
                        Predicate = s => s.Id == input.SubmissionId,
                        IncludeProperties = new List<Expression<Func<Submission, object>>>
                        {
                            s => s.Topic
                        }
                    }
                );

                if (submission == null || submission.Topic == null)
                {
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Submission or associated topic not found."
                    };
                }

                // Step 2: Prepare context for AI embedding
                var submissionContext = string.Join(" ", new[]
                {
                    submission.Topic.Category?.Name,
                    submission.Topic.EN_Title,
                    submission.Topic.VN_title,
                    submission.Topic.Description,
                    submission.Topic.Objectives,
                    submission.Topic.Problem,
                    submission.Topic.Content,
                    submission.Topic.Context
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

                if (string.IsNullOrWhiteSpace(submissionContext))
                {
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Submission context is empty or invalid."
                    };
                }

                // Step 3: Fetch eligible reviewers
                var reviewers = (await _unitOfWork.GetRepo<User>().GetAllAsync(
                    new QueryOptions<User>
                    {
                        IncludeProperties = new List<Expression<Func<User, object>>>
                        {
                            u => u.LecturerSkills,
                            u => u.UserRoles,
                            u => u.ReviewerAssignments,
                            u => u.ReviewerPerformances
                        },
                        Predicate = u => u.UserRoles.Any(r => r.Role != null && r.Role.Name == "Reviewer") && u.LecturerSkills.Any()
                    }
                )).ToList();

                if (!reviewers.Any())
                {
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "No eligible reviewers found. Ensure there are users with the 'Reviewer' role and associated skills in the database."
                    };
                }

                // Step 4: Calculate reviewer scores
                List<ReviewerSuggestionDTO> reviewerScores;
                var skipMessages = new List<string>();
                try
                {
                    var topicFields = new Dictionary<string, string>
                    {
                        { "Title", submission.Topic.EN_Title ?? string.Empty },
                        { "VN_Title", submission.Topic.VN_title ?? string.Empty },
                        { "Category", submission.Topic.Category?.Name ?? string.Empty },
                        { "Description", submission.Topic.Description ?? string.Empty },
                        { "Objectives", submission.Topic.Objectives ?? string.Empty },
                        { "Problem", submission.Topic.Problem ?? string.Empty },
                        { "Content", submission.Topic.Content ?? string.Empty },
                        { "Context", submission.Topic.Context ?? string.Empty }
                    };

                    int? semesterId = submission.Topic?.SemesterId;
                    reviewerScores = await CalculateReviewerScores(reviewers, topicFields, submissionContext, skipMessages, semesterId);
                }
                catch (Exception ex)
                {
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = $"Failed to calculate reviewer scores: {ex.Message}"
                    };
                }

                // Step 5: Sort and prioritize reviewers
                var suggestions = reviewerScores
                    .OrderBy(r => r.CurrentActiveAssignments) // Prioritize by lowest workload
                    .ThenByDescending(r => r.OverallScore)    // Then by overall score
                    .Take(input.MaxSuggestions)              // Limit to max suggestions
                    .ToList();

                // Step 6: Generate AI explanation (optional)
                string? aiExplanation = null;
                if (input.UsePrompt)
                {
                    try
                    {
                        aiExplanation = await GenerateAIExplanation(submissionContext, suggestions);
                    }
                    catch (Exception ex)
                    {
                        // Do not return full exception or provider JSON to the client; log full details for admins
                        _logger.LogWarning(ex, "AI explanation generation failed for SubmissionId {SubmissionId}", input.SubmissionId);
                        aiExplanation = "Failed to generate AI explanation: a provider error occurred; full details have been logged for administrators.";
                    }
                }

                // Step 7: Return the response
                return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                {
                    Data = new ReviewerSuggestionOutputDTO
                    {
                        Suggestions = suggestions,
                        AIExplanation = aiExplanation
                        ,SkipMessages = skipMessages
                    },
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Reviewer suggestions generated successfully."
                };
            }
            catch (Exception ex)
            {
                // Gracefully handle unexpected errors
                return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"An unexpected error occurred: {ex.Message}"
                };
            }
        }

    private async Task<List<ReviewerSuggestionDTO>> CalculateReviewerScores(List<User> reviewers, Dictionary<string, string> topicFields, string submissionContext, List<string> skipMessages, int? semesterId)
        {
            // Validate submission context
            if (string.IsNullOrWhiteSpace(submissionContext))
            {
                throw new ArgumentException("Submission context cannot be null or empty.", nameof(submissionContext));
            }

            // DB-driven tokenization & term-frequency cosine similarity (no external embeddings)
            var reviewerScores = new List<ReviewerSuggestionDTO>();

            // Tokenize and build topic TF map (overall) and per-field TF maps
            var topicTokens = TokenizeText(submissionContext);
            if (!topicTokens.Any())
            {
                throw new ArgumentException("Submission context has no tokens after tokenization.", nameof(submissionContext));
            }

            var topicTf = BuildTermFrequency(topicTokens);
            var topicNorm = ComputeVectorNorm(topicTf);

            // Build per-field TF maps from whatever fields the caller provided (keeps extensions flexible)
            var topicFieldTfs = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in topicFields.Keys)
            {
                topicFields.TryGetValue(key, out var raw);
                var tokens = TokenizeText(raw ?? string.Empty);
                topicFieldTfs[key] = BuildTermFrequency(tokens);
            }

            // Try to generate a topic embedding once (best-effort). If Gemini fails, we continue with TF-only scores.
            float[]? topicEmbedding = null;
            try
            {
                var emb = await _aiService.GetEmbeddingAsync(submissionContext);
                topicEmbedding = emb;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Topic embedding failed - falling back to TF-only similarity");
            }

            foreach (var reviewer in reviewers)
            {
                try
                {
                    // Skip reviewers who currently have too many active assignments (Assigned/InProgress >= 5)
                    var activeCount = reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) ?? 0;
                    if (activeCount >= 5)
                    {
                        var msg = $"Reviewer {reviewer.Id} skipped because current active assignments ({activeCount}) >= 5";
                        _logger.LogDebug(msg);
                        skipMessages?.Add(msg);
                        continue;
                    }
                    // Reviewer skills -> tokens and TF
                    var skills = reviewer.LecturerSkills ?? Enumerable.Empty<LecturerSkill>();
                    if (!(skills?.Any() ?? false))
                    {
                        _logger.LogDebug("Reviewer {ReviewerId} has no LecturerSkills.", reviewer.Id);
                        continue;
                    }

                    // Build reviewer token map (skill tag tokens, weighted by proficiency) and use cache
                    var reviewerSkillDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var s in skills)
                    {
                        if (string.IsNullOrWhiteSpace(s.SkillTag)) continue;
                        var tag = s.SkillTag.Trim();
                        reviewerSkillDict[tag] = s.ProficiencyLevel.ToString();
                    }

                    if (!reviewerSkillDict.Any())
                    {
                        _logger.LogDebug("Reviewer {ReviewerId} has no tokens from skills.", reviewer.Id);
                        continue;
                    }

                    // Try cache
                    Dictionary<string, decimal> reviewerTf;
                    decimal reviewerNorm;
                    var now = DateTime.UtcNow;
                    if (_reviewerTfCache.TryGetValue(reviewer.Id, out var cacheEntry) && cacheEntry.ExpiresAt > now)
                    {
                        reviewerTf = cacheEntry.Tf;
                        reviewerNorm = cacheEntry.Norm;
                    }
                    else
                    {
                        var reviewerTokens = new List<string>();
                        foreach (var kv in reviewerSkillDict)
                        {
                            var repeat = (int)Math.Max(1, (int)skills.First(s => s.SkillTag == kv.Key).ProficiencyLevel);
                            for (int i = 0; i < repeat; i++) reviewerTokens.AddRange(TokenizeText(kv.Key));
                        }

                        reviewerTf = BuildTermFrequency(reviewerTokens);
                        reviewerNorm = ComputeVectorNorm(reviewerTf);
                        _reviewerTfCache[reviewer.Id] = (reviewerTf, reviewerNorm, now.Add(ReviewerTfCacheTtl));
                    }

                    // Compute TF-based cosine similarity (sparse intersection)
                    var dot = 0.0m;
                    foreach (var kv in reviewerTf)
                    {
                        if (topicTf.TryGetValue(kv.Key, out var tcount))
                        {
                            dot += kv.Value * tcount;
                        }
                    }

                    var tfSkillMatch = 0.0m;
                    if (topicNorm > 0 && reviewerNorm > 0)
                    {
                        tfSkillMatch = dot / (topicNorm * reviewerNorm);
                    }

                    // Try semantic embedding cosine if available
                    double semanticScore = 0.0;
                    try
                    {
                        // get or compute reviewer embedding
                        float[]? reviewerEmb = null;
                        var nowUtc = DateTime.UtcNow;
                        if (_reviewerEmbeddingCache.TryGetValue(reviewer.Id, out var cacheEmb) && cacheEmb.ExpiresAt > nowUtc)
                        {
                            reviewerEmb = cacheEmb.Emb;
                        }
                        else
                        {
                            // build a short text from reviewer skills to embed
                            var skillText = string.Join(" ", reviewer.LecturerSkills?.Select(s => s.SkillTag).Where(t => !string.IsNullOrWhiteSpace(t)) ?? Enumerable.Empty<string>());
                            if (!string.IsNullOrWhiteSpace(skillText))
                            {
                                reviewerEmb = await _aiService.GetEmbeddingAsync(skillText);
                                if (reviewerEmb != null && reviewerEmb.Length > 0)
                                {
                                    _reviewerEmbeddingCache[reviewer.Id] = (reviewerEmb, nowUtc.Add(ReviewerEmbeddingCacheTtl));
                                }
                            }
                        }

                        if (topicEmbedding != null && reviewerEmb != null && topicEmbedding.Length == reviewerEmb.Length)
                        {
                            semanticScore = _aiService.CosineSimilarity(topicEmbedding, reviewerEmb);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Reviewer embedding failed for {ReviewerId}", reviewer.Id);
                        semanticScore = 0.0;
                    }

                    // Blend TF and semantic scores; prefer semantic when available but keep TF as robust signal
                    // Both scores normalized in [0,1]; semanticScore may be negative if vectors disagree - clamp
                    var semanticClamped = Math.Max(0.0, Math.Min(1.0, semanticScore));
                    var tfDouble = (double)tfSkillMatch;
                    var blended = topicEmbedding != null && semanticClamped > 0 ? (0.55 * semanticClamped + 0.45 * tfDouble) : tfDouble;
                    var skillMatchScore = (decimal)blended;

                    // Per-field similarity breakdown
                    var fieldScores = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                    var fieldTopTokens = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var fld in topicFieldTfs)
                    {
                        var fldTf = fld.Value;
                        if (fldTf == null || fldTf.Count == 0)
                        {
                            fieldScores[fld.Key] = 0m;
                            fieldTopTokens[fld.Key] = new List<string>();
                            continue;
                        }

                        // dot product for field
                        decimal fldDot = 0m;
                        foreach (var kv in reviewerTf)
                        {
                            if (fldTf.TryGetValue(kv.Key, out var tcount)) fldDot += kv.Value * tcount;
                        }

                        var fldNorm = ComputeVectorNorm(fldTf);
                        var fldScore = (fldNorm > 0 && reviewerNorm > 0) ? fldDot / (fldNorm * reviewerNorm) : 0m;
                        fieldScores[fld.Key] = Decimal.Round(fldScore, 4);

                        // Top matching tokens for this field
                        var tokensIntersection = reviewerTf.Keys.Intersect(fldTf.Keys, StringComparer.OrdinalIgnoreCase)
                            .OrderByDescending(k => reviewerTf[k] * fldTf[k])
                            .Take(8)
                            .ToList();
                        fieldTopTokens[fld.Key] = tokensIntersection;
                    }

                    // Matched skills: find skill tags that have token overlap (using per-field TFs)
                    var matchedSkills = new List<string>();
                    foreach (var tag in reviewerSkillDict.Keys)
                    {
                        var tagTokens = TokenizeText(tag);
                        if (tagTokens.Any(tok => topicTf.ContainsKey(tok))) matchedSkills.Add(tag);
                    }

                    // Prefer DB-stored ReviewerPerformance values for semester-scoped metrics when available
                    int currentActiveAssignments;
                    int completedAssignments;
                    decimal performanceScore;

                    ReviewerPerformance? perf = null;
                    try
                    {
                        if (semesterId.HasValue)
                        {
                            perf = reviewer.ReviewerPerformances?.FirstOrDefault(p => p.SemesterId == semesterId.Value);
                        }
                    }
                    catch { perf = null; }

                    if (perf != null)
                    {
                        // Derive current active from TotalAssignments - CompletedAssignments (ensure non-negative)
                        var active = perf.TotalAssignments - perf.CompletedAssignments;
                        currentActiveAssignments = Math.Max(0, active);
                        completedAssignments = perf.CompletedAssignments;
                        // Build a performance score using QualityRating, OnTimeRate and AverageScoreGiven (fallbacks to 0)
                        var quality = perf.QualityRating ?? 0m;
                        var onTime = perf.OnTimeRate ?? 0m;
                        var avgScore = perf.AverageScoreGiven ?? 0m;
                        performanceScore = quality * 0.5m + onTime * 0.3m + avgScore * 0.2m;
                    }
                    else
                    {
                        // Fallback: compute from in-memory reviewer record
                        currentActiveAssignments = reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) ?? 0;
                        completedAssignments = reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Completed) ?? 0;
                        performanceScore = CalculatePerformanceScore(reviewer);
                    }

                    // Workload score (lower is better) - invert so higher is better (0..1)
                    var workloadScore = 1 - Math.Min(1, currentActiveAssignments / 5m);

                    var overallScore = skillMatchScore * 0.5m + workloadScore * 0.3m + performanceScore * 0.2m;

                    var dto = new ReviewerSuggestionDTO
                    {
                        ReviewerId = reviewer.Id,
                        ReviewerName = reviewer.Profile?.FullName ?? reviewer.Email ?? "Unknown",
                        ReviewerSkills = reviewerSkillDict,
                        MatchedSkills = matchedSkills,
                        SkillMatchScore = Decimal.Round(skillMatchScore, 4),
                        SkillMatchFieldScores = fieldScores,
                        SkillMatchTopTokens = fieldTopTokens,
                        WorkloadScore = Decimal.Round(workloadScore, 4),
                        PerformanceScore = Decimal.Round(performanceScore, 4),
                        OverallScore = Decimal.Round(overallScore, 4),
                        CurrentActiveAssignments = currentActiveAssignments,
                        CompletedAssignments = completedAssignments,
                        // Populate performance fields (prefer DB values when available)
                        AverageScoreGiven = perf?.AverageScoreGiven,
                        OnTimeRate = perf?.OnTimeRate,
                        QualityRating = perf?.QualityRating,
                        IsEligible = matchedSkills.Any() && (reviewer.LecturerSkills?.Any() ?? false),
                        IneligibilityReasons = matchedSkills.Any() ? new List<string>() : new List<string> { "No matching skills with topic" }
                    };

                    // Add small explain string to log for audit
                    _logger.LogDebug("Reviewer {ReviewerId}: overallSkill={OverallSkill} fieldScores={FieldScores}", reviewer.Id, dto.SkillMatchScore, string.Join(',', dto.SkillMatchFieldScores.Select(k => k.Key + ":" + k.Value)));

                    reviewerScores.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to calculate DB-driven score for reviewer {ReviewerId}.", reviewer.Id);
                }
            }

            return reviewerScores;
        }

        // Simple tokenizer: lowercase, remove punctuation, split on whitespace
        private static List<string> TokenizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            // Normalize and remove diacritics
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }

            var cleaned = new string(sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
            var parts = cleaned.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length > 0).ToList();

            // Basic token stemming and synonym expansion
            var unigrams = parts.Where(p => p.Length > 1).Select(StemToken).ToList();

            // include bigrams to catch phrases like "natural language"
            var bigrams = new List<string>();
            for (int i = 0; i + 1 < unigrams.Count; i++)
            {
                bigrams.Add($"{unigrams[i]} {unigrams[i + 1]}");
            }

            var tokens = unigrams.Concat(bigrams).SelectMany(ExpandSynonyms).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return tokens;
        }

        // Expanded synonyms map for acronyms, phrases and common variants (programming, frameworks, infra, data, app types)
        private static readonly Dictionary<string, string[]> _synonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            // Core AI / NLP
            { "nlp", new[]{ "nlp", "natural language processing", "natural language", "language processing", "xử lý ngôn ngữ tự nhiên" } },
            { "natural language processing", new[]{ "natural language processing", "nlp", "xử lý ngôn ngữ tự nhiên" } },
            { "ai", new[]{ "ai", "artificial intelligence", "artificial", "intelligence", "trí tuệ nhân tạo" } },
            { "artificial intelligence", new[]{ "artificial intelligence", "ai", "trí tuệ nhân tạo" } },
            { "ml", new[]{ "ml", "machine learning", "machine", "learning", "học máy" } },
            { "machine learning", new[]{ "machine learning", "ml", "học máy" } },
            { "dl", new[]{ "dl", "deep learning", "deep", "learning", "học sâu" } },
            { "deep learning", new[]{ "deep learning", "dl", "neural networks", "neural", "học sâu", "mạng nơ-ron" } },
            { "cv", new[]{ "cv", "computer vision", "image processing", "vision", "thị giác máy tính", "xử lý ảnh" } },
            { "computer vision", new[]{ "computer vision", "cv", "image processing", "thị giác máy tính" } },
            { "transformer", new[]{ "transformer", "transformers", "bert", "gpt", "llm", "mô hình transformer" } },
            { "bert", new[]{ "bert", "transformer", "mô hình bert" } },
            { "gpt", new[]{ "gpt", "llm", "transformer", "mô hình gpt" } },
            { "llm", new[]{ "llm", "large language model", "language model", "mô hình ngôn ngữ lớn" } },
            { "rnn", new[]{ "rnn", "recurrent neural network", "recurrent", "mạng nơ-ron hồi tiếp" } },
            { "lstm", new[]{ "lstm", "long short term memory", "recurrent", "lstm" } },

            // Data & analytics
            { "data science", new[]{ "data science", "data", "analytics", "data mining", "khoa học dữ liệu" } },
            { "data mining", new[]{ "data mining", "data", "mining", "khai thác dữ liệu" } },
            { "statistics", new[]{ "statistics", "statistical", "probability", "thống kê" } },

            // Engineering / programming
            { "software engineering", new[]{ "software engineering", "software", "engineering", "kỹ thuật phần mềm" } },
            { "programming language", new[]{ "programming language", "programming", "language", "ngôn ngữ lập trình" } },

            // Programming languages
            { "python", new[]{ "python", "py", "python" } },
            { "java", new[]{ "java", "java" } },
            { "csharp", new[]{ "csharp", "c#", "dotnet", "dot net", ".net", "c#" } },
            { "cpp", new[]{ "cpp", "c++", "c plus plus", "cplus", "c++" } },
            { "javascript", new[]{ "javascript", "js", "nodejs", "node", "javascript" } },
            { "typescript", new[]{ "typescript", "ts", "typescript" } },
            { "go", new[]{ "go", "golang", "go" } },
            { "rust", new[]{ "rust", "rust" } },
            { "kotlin", new[]{ "kotlin", "kotlin" } },
            { "swift", new[]{ "swift", "swift" } },
            { "php", new[]{ "php", "php" } },
            { "ruby", new[]{ "ruby", "ruby on rails", "rails", "ruby" } },

            // Frameworks and libraries
            { "aspnet", new[]{ "aspnet", "asp.net", "asp net", "asp", "asp.net" } },
            { "spring", new[]{ "spring", "spring boot", "springboot", "spring-boot", "spring" } },
            { "django", new[]{ "django", "django" } },
            { "flask", new[]{ "flask", "flask" } },
            { "react", new[]{ "react", "reactjs", "react native", "reactnative", "react" } },
            { "angular", new[]{ "angular", "angular" } },
            { "vue", new[]{ "vue", "vuejs", "vue" } },
            { "svelte", new[]{ "svelte", "svelte" } },
            { "nextjs", new[]{ "nextjs", "next", "next.js" } },
            { "nuxt", new[]{ "nuxt", "nuxt" } },
            { "node", new[]{ "node", "nodejs", "node js", "node" } },
            { "express", new[]{ "express", "expressjs", "express" } },
            { "laravel", new[]{ "laravel", "laravel" } },

            // App types and architectures
            { "web app", new[]{ "web app", "web application", "web", "ứng dụng web" } },
            { "mobile", new[]{ "mobile", "mobile app", "android", "ios", "di động", "ứng dụng di động" } },
            { "desktop", new[]{ "desktop", "desktop app", "ứng dụng desktop" } },
            { "microservice", new[]{ "microservice", "microservices", "dịch vụ vi mô", "microservice" } },
            { "rest", new[]{ "rest", "rest api", "restful", "api rest" } },
            { "grpc", new[]{ "grpc", "grpc" } },
            { "graphql", new[]{ "graphql", "graphql" } },

            // Infra / DevOps
            { "docker", new[]{ "docker", "container", "containers", "docker" } },
            { "kubernetes", new[]{ "kubernetes", "k8s", "kube", "kubernetes" } },
            { "helm", new[]{ "helm", "helm" } },
            { "ci/cd", new[]{ "ci/cd", "ci", "cd", "continuous integration", "continuous delivery", "tiếp tục tích hợp", "ci cd" } },

            // Cloud / platforms
            { "aws", new[]{ "aws", "amazon web services", "aws" } },
            { "azure", new[]{ "azure", "microsoft azure", "azure" } },
            { "gcp", new[]{ "gcp", "google cloud", "google cloud platform", "gcp" } },

            // Data / Big Data
            { "etl", new[]{ "etl", "extract transform load", "etl" } },
            { "big data", new[]{ "big data", "hadoop", "spark", "dữ liệu lớn" } },
            { "hadoop", new[]{ "hadoop", "hadoop" } },
            { "spark", new[]{ "spark", "spark" } },

            // Testing / QA
            { "testing", new[]{ "testing", "unit test", "integration test", "tdd", "kiểm thử" } },
            { "tdd", new[]{ "tdd", "test driven development", "phát triển theo kiểm thử" } },

            // Security / blockchain / iot
            { "security", new[]{ "security", "cybersecurity", "information security", "bảo mật" } },
            { "blockchain", new[]{ "blockchain", "distributed ledger", "smart contract", "blockchain" } },
            { "solidity", new[]{ "solidity", "smart contract", "solidity" } },
            { "iot", new[]{ "iot", "internet of things", "embedded", "iot", "vạn vật kết nối" } },

            // Misc common terms
            { "database", new[]{ "database", "sql", "nosql", "mysql", "postgresql", "mongodb", "cơ sở dữ liệu" } },
            { "sql", new[]{ "sql", "structured query language", "sql" } },
            { "nosql", new[]{ "nosql", "document db", "key value", "nosql" } },
            { "algorithm", new[]{ "algorithm", "algorithms", "thuật toán" } },
            { "optimization", new[]{ "optimization", "optimisation", "optimise", "tối ưu hóa" } },
            { "evaluation", new[]{ "evaluation", "metrics", "accuracy", "f1", "precision", "recall", "đánh giá" } }
        };

        private static IEnumerable<string> ExpandSynonyms(string token)
        {
            if (_synonyms.TryGetValue(token, out var list)) return list;
            return new[] { token };
        }

        // Very small stemmer: drop common suffixes -ing, -ed, -s
        private static string StemToken(string token)
        {
            if (token.EndsWith("ing") && token.Length > 5) return token.Substring(0, token.Length - 3);
            if (token.EndsWith("ed") && token.Length > 4) return token.Substring(0, token.Length - 2);
            if (token.EndsWith("s") && token.Length > 3) return token.Substring(0, token.Length - 1);
            return token;
        }

        private static Dictionary<string, decimal> BuildTermFrequency(List<string> tokens)
        {
            var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in tokens)
            {
                if (dict.ContainsKey(t)) dict[t] += 1; else dict[t] = 1;
            }
            return dict;
        }

        private static decimal ComputeVectorNorm(Dictionary<string, decimal> tf)
        {
            decimal sumSquares = 0;
            foreach (var v in tf.Values) sumSquares += v * v;
            return (decimal)Math.Sqrt((double)sumSquares);
        }

        private decimal CalculatePerformanceScore(User reviewer)
        {
            var performance = reviewer.ReviewerPerformances.OrderByDescending(p => p.LastUpdated).FirstOrDefault();
            if (performance == null) return 0;

            return (performance.QualityRating ?? 0) * 0.5m + (performance.OnTimeRate ?? 0) * 0.3m + (performance.AverageScoreGiven ?? 0) * 0.2m;
        }

        private async Task<string?> GenerateAIExplanation(string submissionContext, List<ReviewerSuggestionDTO> suggestions)
        {
            if (suggestions == null || suggestions.Count == 0) return null;

            try
            {
                // Build compact candidates block
                var reviewersLines = suggestions.Select((s, idx) =>
                {
                    var tokens = (s.SkillMatchTopTokens?.Values.SelectMany(v => v) ?? Enumerable.Empty<string>()).Take(6);
                    var matched = (s.MatchedSkills ?? new List<string>()).Take(4);
                    var overallPct = (double)(s.OverallScore * 100);
                    var skillPct = (double)(s.SkillMatchScore * 100);
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0}) Id:{1} | Name:{2} | Active:{3} | Overall:{4:0.0}% | SkillMatch:{5:0.0}% | Matched:[{6}] | Tokens:[{7}]",
                        idx + 1, s.ReviewerId, s.ReviewerName, s.CurrentActiveAssignments, overallPct, skillPct,
                        string.Join(",", matched), string.Join(",", tokens));
                });

                var reviewersBlock = string.Join("\n", reviewersLines);

                // Prompt instructions (concise, strict, bilingual)
                var instruction = new StringBuilder();
                instruction.AppendLine("You are an expert system that recommends reviewers using ONLY the CANDIDATES data and Submission context provided.");
                instruction.AppendLine("Rules:");
                instruction.AppendLine("- Recommend at least TWO reviewers, ordered by lowest CurrentActiveAssignments first. If tied, prefer higher Overall percentage.");
                instruction.AppendLine("- For each recommended reviewer provide a single Recommendation line plus: Overall% and SkillMatch% (format: 0.0%), and a one-line rationale that mentions matched skills/tokens and current active assignments.");
                instruction.AppendLine("- Provide an Alternatives section with two backup reviewers (Name, Id, Active count).");
                instruction.AppendLine("- Output plain text only. First ENGLISH section, then VIETNAMESE translation of the same content.");
                instruction.AppendLine("- Do NOT use JSON, markdown, or code fences. Use percentage values (0.0%) for scores. Keep each language concise (<=180 words).");

                var submissionSnippet = submissionContext.Length > 1000 ? submissionContext.Substring(0, 1000) + "..." : submissionContext;

                var prompt = new StringBuilder();
                prompt.AppendLine(instruction.ToString());
                prompt.AppendLine("Submission context:");
                prompt.AppendLine(submissionSnippet);
                prompt.AppendLine();
                prompt.AppendLine("CANDIDATES:");
                prompt.AppendLine(reviewersBlock);
                prompt.AppendLine();
                prompt.AppendLine("Please respond following the rules above.");

                // Call AI provider
                var aiResponse = await _aiService.GetPromptCompletionAsync(prompt.ToString());
                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    // fallback deterministic answer
                    return BuildDeterministicFallback(suggestions);
                }

                var result = aiResponse.Trim();

                // Strip code fences if present
                if (result.StartsWith("```") && result.EndsWith("```"))
                {
                    var lines = result.Split('\n');
                    var start = (lines.Length > 0 && lines[0].StartsWith("```")) ? 1 : 0;
                    var end = (lines.Length > 1 && lines[lines.Length - 1].StartsWith("```")) ? lines.Length - 1 : lines.Length;
                    result = string.Join('\n', lines.Skip(start).Take(Math.Max(0, end - start))).Trim();
                }

                // If model ignored rules, fallback
                if (!result.Contains("Recommend", StringComparison.OrdinalIgnoreCase) && !result.Contains("Recommendation", StringComparison.OrdinalIgnoreCase))
                {
                    return BuildDeterministicFallback(suggestions);
                }

                _logger.LogDebug("AI explanation generated (preview): {Preview}", result.Length > 200 ? result.Substring(0, 200) + "..." : result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GenerateAIExplanation failed");
                return BuildDeterministicFallback(suggestions, errorNote: true);
            }
        }

        private string BuildDeterministicFallback(List<ReviewerSuggestionDTO> suggestions, bool errorNote = false)
        {
            var ordered = suggestions.OrderBy(r => r.CurrentActiveAssignments).ThenByDescending(r => r.OverallScore).ToList();
            var picks = ordered.Take(2).ToList();

            var sb = new StringBuilder();
            if (errorNote) sb.AppendLine("(Automatic fallback used due to AI error)");

            sb.AppendLine("ENGLISH:");
            foreach (var p in picks)
            {
                var overallPct = (double)(p.OverallScore * 100);
                var skillPct = (double)(p.SkillMatchScore * 100);
                var skills = p.MatchedSkills != null && p.MatchedSkills.Any() ? string.Join(", ", p.MatchedSkills.Take(3)) : "(no matched skills)";
                sb.AppendLine($"Recommendation: {p.ReviewerName} (Id: {p.ReviewerId})");
                sb.AppendLine($"Overall: {overallPct:0.0}% | SkillMatch: {skillPct:0.0}%");
                sb.AppendLine($"Rationale: Low active assignments ({p.CurrentActiveAssignments}) and matched skills: {skills}.");
                sb.AppendLine();
            }
            sb.AppendLine("Alternatives:");
            foreach (var alt in ordered.Skip(2).Take(2)) sb.AppendLine($"- {alt.ReviewerName} (Id: {alt.ReviewerId}) - Active: {alt.CurrentActiveAssignments}");

            sb.AppendLine();
            sb.AppendLine("VIETNAMESE:");
            foreach (var p in picks)
            {
                var overallPct = (double)(p.OverallScore * 100);
                var skillPct = (double)(p.SkillMatchScore * 100);
                var skills = p.MatchedSkills != null && p.MatchedSkills.Any() ? string.Join(", ", p.MatchedSkills.Take(3)) : "(không có kỹ năng khớp)";
                sb.AppendLine($"Đề xuất: {p.ReviewerName} (Id: {p.ReviewerId})");
                sb.AppendLine($"Tổng điểm: {overallPct:0.0}% | Khớp kỹ năng: {skillPct:0.0}%");
                sb.AppendLine($"Lý do: Ít bài đang xử lý ({p.CurrentActiveAssignments}) và kỹ năng khớp: {skills}.");
                sb.AppendLine();
            }
            sb.AppendLine("Phương án thay thế:");
            foreach (var alt in ordered.Skip(2).Take(2)) sb.AppendLine($"- {alt.ReviewerName} (Id: {alt.ReviewerId}) - Số bài đang xử lý: {alt.CurrentActiveAssignments}");

            return sb.ToString().Trim();
        }

        public async Task<BaseResponseModel<ReviewerSuggestionOutputDTO>> SuggestReviewersAsync(ReviewerSuggestionInputDTO input)
        {
            if (input == null)
            {
                return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Input is null"
                };
            }

            try
            {
                // Fetch topic version and its topic
                var topicVersion = await _unitOfWork.GetRepo<TopicVersion>().GetSingleAsync(
                    new QueryOptions<TopicVersion>
                    {
                        Predicate = tv => tv.Id == input.TopicVersionId,
                        IncludeProperties = new List<Expression<Func<TopicVersion, object>>>
                        {
                            tv => tv.Topic
                        }
                    }
                );

                if (topicVersion == null)
                {
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Topic version not found."
                    };
                }

                // Prepare context for embedding
                var topicContext = string.Join(" ", new[]
                {
                    topicVersion.Topic?.Category?.Name,
                    topicVersion.EN_Title,
                    topicVersion.Description,
                    topicVersion.Objectives,
                    topicVersion.Content,
                    topicVersion.Context
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

                if (string.IsNullOrWhiteSpace(topicContext))
                {
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Topic version context is empty or invalid."
                    };
                }

                // Fetch eligible reviewers (same predicate as other methods)
                var reviewers = (await _unitOfWork.GetRepo<User>().GetAllAsync(
                    new QueryOptions<User>
                    {
                        IncludeProperties = new List<Expression<Func<User, object>>>
                        {
                            u => u.LecturerSkills,
                            u => u.UserRoles,
                            u => u.ReviewerAssignments,
                            u => u.ReviewerPerformances
                        },
                        Predicate = u => u.UserRoles.Any(r => r.Role != null && r.Role.Name == "Reviewer") && u.LecturerSkills.Any()
                    }
                )).ToList();

                if (!reviewers.Any())
                {
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "No eligible reviewers found."
                    };
                }

                var topicFields = new Dictionary<string, string>
                {
                    { "Title", topicVersion.EN_Title ?? string.Empty },
                    { "Category", topicVersion.Topic?.Category?.Name ?? string.Empty },
                    { "Description", topicVersion.Description ?? string.Empty },
                    { "Objectives", topicVersion.Objectives ?? string.Empty },
                    { "Content", topicVersion.Content ?? string.Empty },
                    { "Context", topicVersion.Context ?? string.Empty }
                };

                var skipMessages = new List<string>();
                int? semesterId = topicVersion.Topic?.SemesterId;
                var reviewerScores = await CalculateReviewerScores(reviewers, topicFields, topicContext, skipMessages, semesterId);

                var max = input.MaxSuggestions <= 0 ? 5 : input.MaxSuggestions;

                var suggestions = reviewerScores
                    .OrderBy(r => r.CurrentActiveAssignments)
                    .ThenByDescending(r => r.OverallScore)
                    .Take(max)
                    .ToList();

                var aiExplanation = input.UsePrompt ? await GenerateAIExplanation(topicContext, suggestions) : null;

                return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                {
                    Data = new ReviewerSuggestionOutputDTO
                    {
                        Suggestions = suggestions,
                        AIExplanation = aiExplanation
                        ,SkipMessages = skipMessages
                    },
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Reviewer suggestions generated successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SuggestReviewersAsync failed for TopicVersionId {TopicVersionId}", input?.TopicVersionId);
                return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "An unexpected error occurred while generating suggestions."
                };
            }
        }

        public async Task<BaseResponseModel<ReviewerEligibilityDTO>> CheckReviewerEligibilityAsync(int reviewerId, int topicVersionId)
        {
            var reviewer = await _unitOfWork.GetRepo<User>().GetSingleAsync(
                new QueryOptions<User>
                {
                    Predicate = u => u.Id == reviewerId,
                    IncludeProperties = new List<Expression<Func<User, object>>>
                    {
                        u => u.LecturerSkills,
                        u => u.ReviewerAssignments,
                        u => u.ReviewerPerformances
                    }
                });

            if (reviewer == null)
            {
                return new BaseResponseModel<ReviewerEligibilityDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Reviewer not found."
                };
            }

            // Validate topic version exists
            var topicVersion = await _unitOfWork.GetRepo<TopicVersion>().GetSingleAsync(
                new QueryOptions<TopicVersion>
                {
                    Predicate = tv => tv.Id == topicVersionId,
                    IncludeProperties = new List<Expression<Func<TopicVersion, object>>> { tv => tv.Topic }
                });

            if (topicVersion == null)
            {
                return new BaseResponseModel<ReviewerEligibilityDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Topic version not found."
                };
            }

            // Perform eligibility checks (skills, workload, performance)
            var reasons = new List<string>();

                var activeAssignments = reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) ?? 0;
            if (activeAssignments >= 5)
            {
                reasons.Add("Reviewer has too many active assignments.");
            }

            var hasSkills = reviewer.LecturerSkills.Any();
            if (!hasSkills)
            {
                reasons.Add("Reviewer has no recorded skills.");
            }

            var isEligible = !reasons.Any();

            return new BaseResponseModel<ReviewerEligibilityDTO>
            {
                Data = new ReviewerEligibilityDTO
                {
                    ReviewerId = reviewer.Id,
                    TopicVersionId = topicVersionId,
                    TopicId = topicVersion.TopicId,
                    IsEligible = isEligible,
                    IneligibilityReasons = isEligible ? new List<string>() : reasons
                },
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Eligibility check completed successfully."
            };
        }

        public async Task<BaseResponseModel<ReviewerEligibilityDTO>> CheckReviewerEligibilityByTopicIdAsync(int reviewerId, int topicId)
        {
            var reviewer = await _unitOfWork.GetRepo<User>().GetSingleAsync(
                new QueryOptions<User>
                {
                    Predicate = u => u.Id == reviewerId,
                    IncludeProperties = new List<Expression<Func<User, object>>>
                    {
                        u => u.LecturerSkills,
                        u => u.ReviewerAssignments,
                        u => u.ReviewerPerformances
                    }
                });

            if (reviewer == null)
            {
                return new BaseResponseModel<ReviewerEligibilityDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Reviewer not found."
                };
            }
            // Validate topic exists
            var topic = await _unitOfWork.GetRepo<Topic>().GetSingleAsync(new QueryOptions<Topic> { Predicate = t => t.Id == topicId });
            if (topic == null)
            {
                return new BaseResponseModel<ReviewerEligibilityDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Topic not found."
                };
            }

            var reasons = new List<string>();
            var activeAssignments = reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) ?? 0;
            if (activeAssignments >= 5)
            {
                reasons.Add("Reviewer has too many active assignments.");
            }

            var hasSkills = reviewer.LecturerSkills.Any();
            if (!hasSkills)
            {
                reasons.Add("Reviewer has no recorded skills.");
            }

            var isEligible = !reasons.Any();

            return new BaseResponseModel<ReviewerEligibilityDTO>
            {
                Data = new ReviewerEligibilityDTO
                {
                    ReviewerId = reviewer.Id,
                    TopicId = topicId,
                    IsEligible = isEligible,
                    IneligibilityReasons = isEligible ? new List<string>() : reasons
                },
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Eligibility check completed successfully."
            };
        }

        public async Task<BaseResponseModel<ReviewerSuggestionOutputDTO>> SuggestReviewersByTopicIdAsync(ReviewerSuggestionByTopicInputDTO input)
        {
            // Step 1: Fetch the topic and related entities
            var topic = await _unitOfWork.GetRepo<Topic>().GetSingleAsync(
                new QueryOptions<Topic>
                {
                    Predicate = t => t.Id == input.TopicId
                });

            if (topic == null || topic.Category == null)
            {
                return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Topic or topic category not found."
                };
            }

            // Step 2: Prepare context for AI embedding - include the same rich fields used for submissions
            var topicContext = string.Join(" ", new[]
            {
                topic.Category.Name,
                topic.EN_Title,
                // optional Vietnamese title, problem, description, objectives, content and context when present
                // these mirror the fields used when building submissionContext earlier in the service
                topic.VN_title,
                topic.Description,
                topic.Objectives,
                topic.Problem,
                topic.Content,
                topic.Context
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            // Step 3: Fetch eligible reviewers
            var reviewers = (await _unitOfWork.GetRepo<User>().GetAllAsync(
                new QueryOptions<User>
                {
                    IncludeProperties = new List<Expression<Func<User, object>>>
                    {
                        u => u.LecturerSkills,
                        u => u.UserRoles,
                        u => u.ReviewerAssignments,
                        u => u.ReviewerPerformances
                    },
                    Predicate = u => u.UserRoles.Any(r => r.Role != null && r.Role.Name == "Reviewer") && u.LecturerSkills.Any()
                }
            )).ToList();

            if (!reviewers.Any())
            {
                return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "No eligible reviewers found."
                };
            }

            // Step 4: Calculate reviewer scores
                var topicFields = new Dictionary<string, string>
                {
                    { "Title", topic.EN_Title ?? string.Empty },
                    { "Category", topic.Category?.Name ?? string.Empty },
                    { "Description", topic.Description ?? string.Empty },
                    { "Objectives", topic.Objectives ?? string.Empty },
                    { "Content", topic.Content ?? string.Empty },
                    { "Context", topic.Context ?? string.Empty }
                };

                var skipMessages = new List<string>();
                int? semesterId = topic.SemesterId;
                var reviewerScores = await CalculateReviewerScores(reviewers, topicFields, topicContext, skipMessages, semesterId);

            // Step 5: Sort and prioritize reviewers
            var suggestions = reviewerScores
                .OrderBy(r => r.CurrentActiveAssignments) // Prioritize by lowest workload
                .ThenByDescending(r => r.OverallScore)    // Then by overall score
                .Take(input.MaxSuggestions)              // Limit to max suggestions
                .ToList();

            // Step 6: Generate AI explanation (optional)
            var aiExplanation = input.UsePrompt ? await GenerateAIExplanation(topicContext, suggestions) : null;

            // Step 7: Return the response
            return new BaseResponseModel<ReviewerSuggestionOutputDTO>
            {
                Data = new ReviewerSuggestionOutputDTO
                {
                    Suggestions = suggestions,
                    AIExplanation = aiExplanation
                    ,SkipMessages = skipMessages
                },
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Reviewer suggestions generated successfully."
            };
        }
    }
}