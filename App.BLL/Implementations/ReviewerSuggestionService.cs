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
                    submission.Topic.Description,
                    submission.Topic.Objectives
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
                try
                {
                    var topicFields = new Dictionary<string, string>
                    {
                        { "Title", submission.Topic.EN_Title ?? string.Empty },
                        { "Category", submission.Topic.Category?.Name ?? string.Empty },
                        { "Description", submission.Topic.Description ?? string.Empty },
                        { "Objectives", submission.Topic.Objectives ?? string.Empty },
                        { "Content", string.Empty },
                        { "Context", string.Empty }
                    };

                    reviewerScores = await CalculateReviewerScores(reviewers, topicFields, submissionContext);
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

    private Task<List<ReviewerSuggestionDTO>> CalculateReviewerScores(List<User> reviewers, Dictionary<string, string> topicFields, string submissionContext)
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

            // Use explicit topicFields passed from caller (Title, Category, Description, Objectives, Content, Context)
            var topicFieldTfs = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in new[] { "Title", "Category", "Description", "Objectives", "Content", "Context" })
            {
                topicFields.TryGetValue(key, out var raw);
                var tokens = TokenizeText(raw ?? string.Empty);
                topicFieldTfs[key] = BuildTermFrequency(tokens);
            }

            foreach (var reviewer in reviewers)
            {
                try
                {
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

                    // Compute cosine similarity using sparse intersection (overall)
                    var dot = 0.0m;
                    foreach (var kv in reviewerTf)
                    {
                        if (topicTf.TryGetValue(kv.Key, out var tcount))
                        {
                            dot += kv.Value * tcount;
                        }
                    }

                    var skillMatchScore = 0.0m;
                    if (topicNorm > 0 && reviewerNorm > 0)
                    {
                        skillMatchScore = dot / (topicNorm * reviewerNorm);
                    }

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

                    // Workload and performance
                    var workloadScore = CalculateWorkloadScore(reviewer);
                    var performanceScore = CalculatePerformanceScore(reviewer);

                    // Completed assignments count
                    var completedAssignments = reviewer.ReviewerAssignments.Count(a => a.Status == AssignmentStatus.Completed);

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
                        CurrentActiveAssignments = reviewer.ReviewerAssignments.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress),
                        CompletedAssignments = completedAssignments,
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

            return Task.FromResult(reviewerScores);
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
            { "nlp", new[]{ "nlp", "natural language processing", "natural language", "language processing" } },
            { "natural language processing", new[]{ "natural language processing", "nlp" } },
            { "ai", new[]{ "ai", "artificial intelligence", "artificial", "intelligence" } },
            { "artificial intelligence", new[]{ "artificial intelligence", "ai" } },
            { "ml", new[]{ "ml", "machine learning", "machine", "learning" } },
            { "machine learning", new[]{ "machine learning", "ml" } },
            { "dl", new[]{ "dl", "deep learning", "deep", "learning" } },
            { "deep learning", new[]{ "deep learning", "dl", "neural networks", "neural" } },
            { "cv", new[]{ "cv", "computer vision", "image processing", "vision" } },
            { "computer vision", new[]{ "computer vision", "cv", "image processing" } },
            { "transformer", new[]{ "transformer", "transformers", "bert", "gpt", "llm" } },
            { "bert", new[]{ "bert", "transformer" } },
            { "gpt", new[]{ "gpt", "llm", "transformer" } },
            { "llm", new[]{ "llm", "large language model", "language model" } },
            { "rnn", new[]{ "rnn", "recurrent neural network", "recurrent" } },
            { "lstm", new[]{ "lstm", "long short term memory", "recurrent" } },
            { "data science", new[]{ "data science", "data", "analytics", "data mining" } },
            { "data mining", new[]{ "data mining", "data", "mining" } },
            { "statistics", new[]{ "statistics", "statistical", "probability" } },
            { "software engineering", new[]{ "software engineering", "software", "engineering" } },
            { "programming language", new[]{ "programming language", "programming", "language" } },

            // Programming languages
            { "python", new[]{ "python", "py" } },
            { "java", new[]{ "java" } },
            { "csharp", new[]{ "csharp", "c#", "dotnet", "dot net", ".net" } },
            { "cpp", new[]{ "cpp", "c++", "c plus plus", "cplus" } },
            { "javascript", new[]{ "javascript", "js", "nodejs", "node" } },
            { "typescript", new[]{ "typescript", "ts" } },
            { "go", new[]{ "go", "golang" } },
            { "rust", new[]{ "rust" } },
            { "kotlin", new[]{ "kotlin" } },
            { "swift", new[]{ "swift" } },
            { "php", new[]{ "php" } },
            { "ruby", new[]{ "ruby", "ruby on rails", "rails" } },

            // Frameworks and libraries
            { "aspnet", new[]{ "aspnet", "asp.net", "asp net", "asp" } },
            { "spring", new[]{ "spring", "spring boot", "springboot", "spring-boot" } },
            { "django", new[]{ "django" } },
            { "flask", new[]{ "flask" } },
            { "react", new[]{ "react", "reactjs", "react native", "reactnative" } },
            { "angular", new[]{ "angular" } },
            { "vue", new[]{ "vue", "vuejs" } },
            { "svelte", new[]{ "svelte" } },
            { "nextjs", new[]{ "nextjs", "next" } },
            { "nuxt", new[]{ "nuxt" } },
            { "node", new[]{ "node", "nodejs", "node js" } },
            { "express", new[]{ "express", "expressjs" } },
            { "laravel", new[]{ "laravel" } },

            // App types and architectures
            { "web app", new[]{ "web app", "web application", "web" } },
            { "mobile", new[]{ "mobile", "mobile app", "android", "ios" } },
            { "desktop", new[]{ "desktop", "desktop app" } },
            { "microservice", new[]{ "microservice", "microservices" } },
            { "rest", new[]{ "rest", "rest api", "restful" } },
            { "grpc", new[]{ "grpc" } },
            { "graphql", new[]{ "graphql" } },

            // Infra / DevOps
            { "docker", new[]{ "docker", "container", "containers" } },
            { "kubernetes", new[]{ "kubernetes", "k8s", "kube" } },
            { "helm", new[]{ "helm" } },
            { "ci/cd", new[]{ "ci/cd", "ci", "cd", "continuous integration", "continuous delivery" } },

            // Cloud / platforms
            { "aws", new[]{ "aws", "amazon web services" } },
            { "azure", new[]{ "azure", "microsoft azure" } },
            { "gcp", new[]{ "gcp", "google cloud", "google cloud platform" } },

            // Data / Big Data
            { "etl", new[]{ "etl", "extract transform load" } },
            { "big data", new[]{ "big data", "hadoop", "spark" } },
            { "hadoop", new[]{ "hadoop" } },
            { "spark", new[]{ "spark" } },

            // Testing / QA
            { "testing", new[]{ "testing", "unit test", "integration test", "tdd" } },
            { "tdd", new[]{ "tdd", "test driven development" } },

            // Security / blockchain / iot
            { "security", new[]{ "security", "cybersecurity", "information security" } },
            { "blockchain", new[]{ "blockchain", "distributed ledger", "smart contract" } },
            { "solidity", new[]{ "solidity", "smart contract" } },
            { "iot", new[]{ "iot", "internet of things", "embedded" } },

            // Misc common terms
            { "database", new[]{ "database", "sql", "nosql", "mysql", "postgresql", "mongodb" } },
            { "sql", new[]{ "sql", "structured query language" } },
            { "nosql", new[]{ "nosql", "document db", "key value" } },
            { "algorithm", new[]{ "algorithm", "algorithms" } },
            { "optimization", new[]{ "optimization", "optimisation", "optimise" } },
            { "evaluation", new[]{ "evaluation", "metrics", "accuracy", "f1", "precision", "recall" } }
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

        // Heuristic extractor: tries to find "FieldName: ..." patterns; returns whole text if not found
        private static string? ExtractField(string text, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            try
            {
                var lower = text.ToLowerInvariant();
                var marker = fieldName.ToLowerInvariant() + ":";
                var idx = lower.IndexOf(marker, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var start = idx + marker.Length;
                    // take up to next double newline or end
                    var rest = text.Substring(start).Trim();
                    var sep = rest.IndexOf("\n\n");
                    if (sep > 0) return rest.Substring(0, sep).Trim();
                    // fallback: up to 300 chars
                    return rest.Length > 300 ? rest.Substring(0, 300) : rest;
                }
            }
            catch { }
            // if not found, return null so caller can fall back
            return null;
        }

        private decimal CalculateWorkloadScore(User reviewer)
        {
            var activeAssignments = reviewer.ReviewerAssignments.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress);
            return 1 - Math.Min(1, activeAssignments / 5m);
        }

        private decimal CalculatePerformanceScore(User reviewer)
        {
            var performance = reviewer.ReviewerPerformances.OrderByDescending(p => p.LastUpdated).FirstOrDefault();
            if (performance == null) return 0;

            return (performance.QualityRating ?? 0) * 0.5m + (performance.OnTimeRate ?? 0) * 0.3m + (performance.AverageScoreGiven ?? 0) * 0.2m;
        }

        private async Task<string?> GenerateAIExplanation(string submissionContext, List<ReviewerSuggestionDTO> suggestions)
        {
            try
            {
                // Build a structured, professional prompt including per-field match details
                var reviewersText = string.Join("\n\n", suggestions.Select((s, i) =>
                {
                    var fieldsInfo = string.Join("; ", s.SkillMatchFieldScores.Select(kv => $"{kv.Key}:{kv.Value:F2}"));
                    var topTokensInfo = string.Join(",", s.SkillMatchTopTokens.SelectMany(kv => kv.Value.Select(t => $"{kv.Key}:{t}")));
                    return $"Reviewer {i + 1}: {s.ReviewerName} (ID:{s.ReviewerId})\nOverallScore: {s.OverallScore:F3}\nSkillMatchScore: {s.SkillMatchScore:F3}\nFieldScores: {fieldsInfo}\nTopTokens: {topTokensInfo}\nWorkload: {s.WorkloadScore:F3}\nPerformance: {s.PerformanceScore:F3}\nMatchedSkills: {string.Join(",", s.MatchedSkills)}";
                }));

                var jsonSchema = "{\"reviewers\": [{ \"id\": number, \"recommendation\": \"string\", \"reasons\": [\"string\"], \"concerns\": [\"string\"], \"top_matching_tokens\": [\"string\"] }], \"overall_recommendation\": \"string\" }";

                var basePrompt = $@"You are an expert academic reviewer recommender. Given the submission context and the compiled reviewer profiles below, produce a concise, professional explanation (3-5 bullet points each) why each reviewer is suitable. Include any potential concerns (workload, missing skill coverage) and suggest the top 1-2 best matches.

Submission context:
---
{submissionContext}
---

Reviewer summaries:
{reviewersText}

Respond in JSON with schema: {jsonSchema}";

                // If the prompt is too long, truncate the submissionContext portion and note truncation
                const int maxPromptLength = 3000; // conservative safe limit
                string promptToSend = basePrompt;
                if (basePrompt.Length > maxPromptLength)
                {
                    // Truncate submissionContext while preserving reviewer list and instruction
                    var note = "[Truncated submission context to fit model limits]";
                    var allowedContextLen = Math.Max(200, maxPromptLength - reviewersText.Length - note.Length - 200);
                    var truncatedContext = submissionContext.Length > allowedContextLen ? submissionContext.Substring(0, allowedContextLen) + "..." : submissionContext;
                    promptToSend = $"Given the submission context:\n---\n{truncatedContext}\n---\n{note}\n\nand these reviewer profiles:\n{reviewersText}\nExplain why these reviewers are suitable.";
                }

                return await _aiService.GetPromptCompletionAsync(promptToSend);
            }
            catch (Exception ex)
            {
                // Log full exception for administrators, but return a concise, non-sensitive message to the API client
                _logger.LogWarning(ex, "Prompt completion failed for submission context");

                var full = ex.ToString();
                var summary = ex.Message ?? "Prompt generation failed";

                // Remove any JSON payload from the summary to avoid leaking provider responses
                var idx = summary.IndexOf('{');
                if (idx > 0)
                {
                    summary = summary.Substring(0, idx).Trim();
                }

                // Detect common rate-limit/quota signals from provider responses
                var isRateLimit = full.Contains("TooManyRequests") || full.Contains("RESOURCE_EXHAUSTED") || full.Contains("Quota exceeded") || full.Contains("429");

                var howToFix = isRateLimit
                    ? "How to fix: retry after a delay (respect Retry-After header), reduce request rate or batch requests, or request a quota increase in Google Cloud Console; consider caching or background queueing."
                    : "How to fix: check provider response and logs, implement retries/backoff for transient errors, and ensure request payloads are valid.";

                // Return a concise message; full provider error is kept only in logs
                var result = $"Failed to generate AI explanation: {summary}. {howToFix} Full provider error has been logged for administrators.";
                return result;
            }
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

                var reviewerScores = await CalculateReviewerScores(reviewers, topicFields, topicContext);

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

            var activeAssignments = reviewer.ReviewerAssignments.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress);
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
            var activeAssignments = reviewer.ReviewerAssignments.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress);
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

            // Step 2: Prepare context for AI embedding
            var topicContext = string.Join(" ", new[]
            {
                topic.Category.Name,
                topic.EN_Title
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
                    { "Content", string.Empty },
                    { "Context", string.Empty }
                };

                var reviewerScores = await CalculateReviewerScores(reviewers, topicFields, topicContext);

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
                },
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Reviewer suggestions generated successfully."
            };
        }
    }
}