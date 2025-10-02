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
using System.Text.Json;
using System.Text.Json.Serialization;
using App.BLL.Services; // For GeminiAIService and VectorMath
using Microsoft.Extensions.Logging;

#nullable enable

namespace App.BLL.Implementations
{
    public class ReviewerSuggestionService : IReviewerSuggestionService
    {
    private readonly GeminiAIService _aiService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReviewerSuggestionService> _logger;
    private readonly Microsoft.Extensions.Hosting.IHostApplicationLifetime _appLifetime;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, (float[] Emb, DateTime ExpiresAt)> _reviewerEmbeddingCache = new();
        private static readonly TimeSpan ReviewerEmbeddingCacheTtl = TimeSpan.FromHours(1);
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (float[] Emb, DateTime ExpiresAt)> _skillEmbeddingCache = new();
        private static readonly TimeSpan SkillEmbeddingCacheTtl = TimeSpan.FromDays(7);
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (float[] Emb, DateTime ExpiresAt)> _fieldEmbeddingCache = new();
        private static readonly TimeSpan FieldEmbeddingCacheTtl = TimeSpan.FromDays(7);
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (float[] Emb, DateTime ExpiresAt)> _tokenEmbeddingCache = new();
        private static readonly TimeSpan TokenEmbeddingCacheTtl = TimeSpan.FromDays(30);

    // Thresholds used for semantic matching. Tweak carefully and re-run integration tests when changed.
    // TokenMatchThreshold: minimum cosine similarity between a token n-gram and a reviewer skill embedding
    // to consider that token as evidence the reviewer knows that token/topic fragment.
    private const double TokenMatchThreshold = 0.45; // conservative: ~0.4-0.5 works well for many embedding models

    // FieldMatchThreshold: minimum cosine similarity between a field (title/description) embedding and a skill
    // to treat that skill as relevant to that specific field. Prevents unrelated skills from surfacing tokens.
    private const double FieldMatchThreshold = 0.30; // allow weaker per-field relevance than full-skill-topic match

    // SkillTagMatchThreshold: minimum cosine similarity between a skill tag and the topic embedding to mark the
    // skill as a matched skill for the reviewer (strong signal).
    private const double SkillTagMatchThreshold = 0.60; // fairly strict: prefer explicit skill matches

    // EligibilityEmbeddingThreshold: fallback reviewer embedding vs topic embedding threshold used to decide
    // eligibility when no explicit matched skills exist. Keep moderate to avoid false positives.
    private const double EligibilityEmbeddingThreshold = 0.25;

        public ReviewerSuggestionService(GeminiAIService aiService, IUnitOfWork unitOfWork, ILogger<ReviewerSuggestionService> logger, Microsoft.Extensions.Hosting.IHostApplicationLifetime appLifetime)
        {
            _aiService = aiService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _appLifetime = appLifetime;
        }

        public async Task<BaseResponseModel<ReviewerSuggestionOutputDTO>> SuggestReviewersBySubmissionIdAsync(ReviewerSuggestionBySubmissionInputDTO input)
        {
            try
            {
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

                // Step A: validate the submission and its topic exist. If not, return 404 early.
                if (submission == null || submission.Topic == null)
                {
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Submission or associated topic not found."
                    };
                }

                var submissionContext = string.Join(" ", new[]
                {
                    submission.Topic.Category?.Description,
                    submission.Topic.Category?.Name,
                    submission.Topic.EN_Title,
                    submission.Topic.VN_title,
                    submission.Topic.Description,
                    submission.Topic.Objectives,
                    submission.Topic.Problem,
                    submission.Topic.Content,
                    submission.Topic.Context
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

                // Step B: ensure we have a non-empty textual context extracted from topic fields.
                // This concatenated context is the primary input to the embedding and matching pipeline.
                if (string.IsNullOrWhiteSpace(submissionContext))
                {
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Submission context is empty or invalid."
                    };
                }

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

                // Step C: ensure we have candidate reviewers loaded with their skills, roles and past performance.
                // If no reviewer with the 'Reviewer' role and at least one LecturerSkill exists, abort early.
                if (!reviewers.Any())
                {
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "No eligible reviewers found. Ensure there are users with the 'Reviewer' role and associated skills in the database."
                    };
                }

                var topicFields = new Dictionary<string, string>
                {
                    { "Title", submission.Topic.EN_Title ?? string.Empty },
                    { "VN_Title", submission.Topic.VN_title ?? string.Empty },
                    { "Category", (submission.Topic.Category?.Name ?? string.Empty) },
                    { "Description", submission.Topic.Description ?? string.Empty },
                    { "Objectives", submission.Topic.Objectives ?? string.Empty },
                    { "Problem", submission.Topic.Problem ?? string.Empty },
                    { "Content", submission.Topic.Content ?? string.Empty },
                    { "Context", submission.Topic.Context ?? string.Empty }
                };

                foreach (var k in topicFields.Keys.ToList())
                {
                    var v = topicFields[k] ?? string.Empty;
                    if (v.Length < 10)
                    {
                        var extra = submissionContext.Length > 200 ? submissionContext.Substring(0, 200) : submissionContext;
                        topicFields[k] = (v + " " + extra).Trim();
                    }
                }

                var skipMessages = new List<string>();
                int? semesterId = submission.Topic?.SemesterId;
                // Step D: compute semantic scores for every candidate reviewer. This is the heavy part:
                //  - build/lookup embeddings for topic fields and skills
                //  - derive matched skills per reviewer
                //  - extract top tokens that justify the match
                //  - compute workload, performance and overall scores
                List<ReviewerSuggestionDTO> reviewerScores;

                try
                {
                    reviewerScores = await CalculateReviewerScores(reviewers, topicFields, submissionContext, skipMessages, semesterId);
                }
                catch (Exception ex)
                {
                    // If scoring fails, return a 500 explaining the failure. The caller can decide to retry.
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = $"Failed to calculate reviewer scores: {ex.Message}"
                    };
                }

                // Step E: sort suggestions by workload (fewest active assignments) and then by overall score.
                // Finally, limit to MaxSuggestions requested by caller.
                var suggestions = reviewerScores
                    .OrderBy(r => r.CurrentActiveAssignments)
                    .ThenByDescending(r => r.OverallScore)
                    .Take(input.MaxSuggestions)
                    .ToList();

                // Step F: optionally generate a compact AI explanation that references only the chosen candidates.
                // We deliberately catch quota exceptions to avoid cascading provider errors and to allow the API to return partial results.
                string? aiExplanation = null;
                if (input.UsePrompt)
                {
                    try
                    {
                        aiExplanation = await GenerateAIExplanation(submissionContext, suggestions);
                    }
                    catch (App.BLL.Services.AIQuotaExceededException qex)
                    {
                        // Quota hit: log and add a skip message. We also attempt a graceful stop to prevent further AI calls.
                        _logger.LogError(qex, "AI quota exceeded while generating explanation for SubmissionId {SubmissionId}", input.SubmissionId);
                        skipMessages.Add("AI provider quota exceeded; stopping AI calls. Administrator intervention required.");
                        // Stop the application gracefully to avoid further API calls
                        try { _appLifetime.StopApplication(); } catch { }
                        aiExplanation = "AI quota exceeded; explanation unavailable.";
                    }
                    catch (Exception ex)
                    {
                        // Other AI errors are non-fatal for the API call; we return suggestions without the AI text.
                        _logger.LogWarning(ex, "AI explanation generation failed for SubmissionId {SubmissionId}", input.SubmissionId);
                        aiExplanation = "Failed to generate AI explanation: a provider error occurred; full details have been logged for administrators.";
                    }
                }

                return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                {
                    Data = new ReviewerSuggestionOutputDTO
                    {
                        Suggestions = suggestions,
                        AIExplanation = aiExplanation,
                        SkipMessages = skipMessages
                    },
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Reviewer suggestions generated successfully."
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"An unexpected error occurred: {ex.Message}"
                };
            }
        }

        private static string NormalizeVietnamese(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            // Step 1: Unicode normalization (decompose accents) so we can remove combining marks
            // This helps convert Vietnamese text to a stable form for tokenization/embedding calls.
            s = s.Normalize(NormalizationForm.FormKD);
            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            var cleaned = sb.ToString().ToLowerInvariant();
            // Remove punctuation so tokens become continuous words and phrases.
            cleaned = new string(cleaned.Where(c => !char.IsPunctuation(c)).ToArray());
            return cleaned;
        }

        private static IEnumerable<string> GenerateNgrams(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            var tokens = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Where(t => t.Length > 0).ToArray();
            for (int n = 1; n <= 3; n++)
            {
                for (int i = 0; i + n <= tokens.Length; i++)
                {
                    yield return string.Join(' ', tokens, i, n);
                }
            }
        }

        // Produce pairs of (normalized, original) n-grams preserving positions so we can embed normalized text
        // but show human-readable original n-grams in outputs.
        private static IEnumerable<(string Normalized, string Original)> GenerateNgramPairs(string originalText)
        {
            if (string.IsNullOrWhiteSpace(originalText)) yield break;
            var origTokens = originalText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Where(t => t.Length > 0).ToArray();
            var normTokens = origTokens.Select(t => NormalizeVietnamese(t)).ToArray();
            for (int n = 1; n <= 3; n++)
            {
                for (int i = 0; i + n <= origTokens.Length; i++)
                {
                    var orig = string.Join(' ', origTokens, i, n);
                    var norm = string.Join(' ', normTokens, i, n);
                    // Return both normalized and original token so we can embed the normalized form
                    // while showing the original form in human-readable outputs.
                    yield return (norm, orig);
                }
            }
        }

        // Basic Vietnamese stopword list and token quality checks to avoid tiny fragments like "ung", "dung".
        private static readonly HashSet<string> _vnStopwords = new(StringComparer.OrdinalIgnoreCase)
        {
            "và","của","là","có","cho","với","trong","một","những","được","từ","để","trên","về","các","một","nhưng","này","đó"
        };

        private static bool IsMeaningfulToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var parts = token.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // require at least one part with length >=3 and at least one alphabetic character
            if (!parts.Any(p => p.Any(char.IsLetter))) return false;
            if (!parts.Any(p => p.Length >= 3)) return false;
            // no stopword-only tokens
            if (parts.All(p => _vnStopwords.Contains(p))) return false;
            return true;
        }

        private async Task<List<ReviewerSuggestionDTO>> CalculateReviewerScores(List<User> reviewers, Dictionary<string, string> topicFields, string submissionContext, List<string> skipMessages, int? semesterId)
        {
            if (string.IsNullOrWhiteSpace(submissionContext)) throw new ArgumentException("Submission context cannot be null or empty.", nameof(submissionContext));

            var nowUtc = DateTime.UtcNow;
            var results = new List<ReviewerSuggestionDTO>();

            var normFields = topicFields.ToDictionary(k => k.Key, v => NormalizeVietnamese((v.Value ?? string.Empty) + " " + submissionContext.Substring(0, Math.Min(200, submissionContext.Length))), StringComparer.OrdinalIgnoreCase);

            var fieldEmbeddings = new Dictionary<string, float[]>();
            foreach (var kv in normFields)
            {
                var key = kv.Key + "|" + kv.Value;
                // Try cache first: field embeddings are expensive and shared across reviewers
                if (_fieldEmbeddingCache.TryGetValue(key, out var ce) && ce.ExpiresAt > nowUtc)
                {
                    fieldEmbeddings[kv.Key] = ce.Emb;
                    continue;
                }
                try
                {
                    var emb = await _aiService.GetEmbeddingAsync(kv.Value);
                    if (emb != null && emb.Length > 0)
                    {
                        fieldEmbeddings[kv.Key] = emb;
                        _fieldEmbeddingCache[key] = (emb, nowUtc.Add(FieldEmbeddingCacheTtl));
                    }
                }
                catch { }
            }

            float[]? topicEmb = null;
            try
            {
                var topicKey = "topic|" + submissionContext;
                if (_fieldEmbeddingCache.TryGetValue(topicKey, out var tcache) && tcache.ExpiresAt > nowUtc) topicEmb = tcache.Emb;
                else if (fieldEmbeddings.Any())
                {
                    var dim = fieldEmbeddings.First().Value.Length;
                    var acc = new float[dim]; int c = 0;
                    foreach (var fe in fieldEmbeddings.Values)
                    {
                        if (fe.Length != dim) continue;
                        for (int i = 0; i < dim; i++) acc[i] += fe[i]; c++;
                    }
                    if (c > 0) { for (int i = 0; i < dim; i++) acc[i] /= c; topicEmb = acc; _fieldEmbeddingCache[topicKey] = (acc, nowUtc.Add(FieldEmbeddingCacheTtl)); }
                }
                else
                {
                    topicEmb = await _aiService.GetEmbeddingAsync(submissionContext);
                }
            }
            catch { }

            // --- Optimization: precompute skill embeddings and reviewer embeddings once for all reviewers ---
            var uniqueSkillTags = reviewers
                .SelectMany(r => r.LecturerSkills ?? Enumerable.Empty<LecturerSkill>())
                .Select(s => s.SkillTag)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => NormalizeVietnamese(t!.Trim()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var skillEmbMapGlobal = new System.Collections.Concurrent.ConcurrentDictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);

            // Limit concurrent embedding requests to avoid bursting provider
            var sem = new System.Threading.SemaphoreSlim(6);
            var skillTasks = new List<Task>();
            foreach (var sk in uniqueSkillTags)
            {
                skillTasks.Add(Task.Run(async () =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        var key = sk;
                        if (_skillEmbeddingCache.TryGetValue(key, out var sc) && sc.ExpiresAt > nowUtc)
                        {
                            skillEmbMapGlobal[key] = sc.Emb;
                            return;
                        }
                        try
                        {
                            var emb = await _aiService.GetEmbeddingAsync(key);
                            if (emb != null && emb.Length > 0)
                            {
                                skillEmbMapGlobal[key] = emb;
                                _skillEmbeddingCache[key] = (emb, nowUtc.Add(SkillEmbeddingCacheTtl));
                            }
                        }
                        catch { }
                    }
                    finally { sem.Release(); }
                }));
            }
            await Task.WhenAll(skillTasks);

            // Build reviewer embeddings by averaging their skill embeddings (avoids extra embedding calls per reviewer)
            var reviewerEmbMap = new Dictionary<int, float[]>();
            foreach (var reviewer in reviewers)
            {
                var sTags = (reviewer.LecturerSkills ?? Enumerable.Empty<LecturerSkill>())
                    .Select(s => s.SkillTag)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => NormalizeVietnamese(t!.Trim()))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                List<float[]> vecs = new List<float[]>();
                foreach (var t in sTags)
                {
                    if (skillEmbMapGlobal.TryGetValue(t, out var se)) vecs.Add(se);
                }
                if (vecs.Count > 0)
                {
                    var dim = vecs[0].Length;
                    var acc = new float[dim];
                    int c = 0;
                    foreach (var v in vecs)
                    {
                        if (v.Length != dim) continue;
                        for (int i = 0; i < dim; i++) acc[i] += v[i];
                        c++;
                    }
                    if (c > 0)
                    {
                        for (int i = 0; i < dim; i++) acc[i] /= c;
                        reviewerEmbMap[reviewer.Id] = acc;
                        _reviewerEmbeddingCache[reviewer.Id] = (acc, nowUtc.Add(ReviewerEmbeddingCacheTtl));
                    }
                }
            }

            // --- Precompute token candidates per field and fetch token embeddings once ---
            var fieldTokenCandidates = new Dictionary<string, List<(string Norm, string Orig)>>(StringComparer.OrdinalIgnoreCase);
            var allNormTokens = new System.Collections.Concurrent.ConcurrentBag<string>();
            foreach (var fk in topicFields.Keys)
            {
                var fieldText = topicFields.ContainsKey(fk) ? (topicFields[fk] ?? string.Empty) : string.Empty;
                var list = new List<(string Norm, string Orig)>();
                int count = 0;
                foreach (var (norm, orig) in GenerateNgramPairs(fieldText))
                {
                    if (count++ >= 24) break;
                    if (string.IsNullOrWhiteSpace(orig)) continue;
                    var display = string.Join(' ', orig.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
                    if (!IsMeaningfulToken(display)) continue;
                    list.Add((norm, display));
                    allNormTokens.Add(norm);
                }
                fieldTokenCandidates[fk] = list;
            }

            // Fetch token embeddings in parallel with semaphore
            var normTokenSet = allNormTokens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var tokenTasks = new List<Task>();
            foreach (var nt in normTokenSet)
            {
                tokenTasks.Add(Task.Run(async () =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        if (_tokenEmbeddingCache.TryGetValue(nt, out var te) && te.ExpiresAt > nowUtc) return;
                        try
                        {
                            var tEmb = await _aiService.GetEmbeddingAsync(nt);
                            if (tEmb != null && tEmb.Length > 0)
                            {
                                _tokenEmbeddingCache[nt] = (tEmb, nowUtc.Add(TokenEmbeddingCacheTtl));
                            }
                        }
                        catch { }
                    }
                    finally { sem.Release(); }
                }));
            }
            await Task.WhenAll(tokenTasks);

            // Precompute skill<->field similarities for available skill embeddings (global)
            var skillFieldSimGlobal = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
            var skillTopicSimGlobal = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (topicEmb != null)
            {
                foreach (var kv in skillEmbMapGlobal)
                {
                    var s = kv.Key; var emb = kv.Value;
                    if (emb == null || emb.Length != topicEmb.Length) continue;
                    var simToTopic = _aiService.CosineSimilarity(topicEmb, emb);
                    simToTopic = Math.Max(0.0, Math.Min(1.0, simToTopic));
                    skillTopicSimGlobal[s] = simToTopic;

                    var fld = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    foreach (var fk in fieldEmbeddings.Keys)
                    {
                        var fe = fieldEmbeddings[fk];
                        if (fe == null || fe.Length != emb.Length) { fld[fk] = 0.0; continue; }
                        var sim = _aiService.CosineSimilarity(fe, emb);
                        sim = Math.Max(0.0, Math.Min(1.0, sim));
                        fld[fk] = sim;
                    }
                    skillFieldSimGlobal[s] = fld;
                }
            }

            foreach (var reviewer in reviewers)
            {
                try
                {
                    var activeCount = reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) ?? 0;
                    if (activeCount >= 5) { skipMessages?.Add($"Reviewer {reviewer.Id} skipped: overloaded ({activeCount})"); continue; }

                    var skills = reviewer.LecturerSkills ?? Enumerable.Empty<LecturerSkill>();
                    if (!skills.Any()) continue;

                    var skillTags = skills.Where(s => !string.IsNullOrWhiteSpace(s.SkillTag)).Select(s => NormalizeVietnamese(s.SkillTag.Trim())).Distinct().ToArray();
                    float[]? reviewerEmb = null;
                    if (_reviewerEmbeddingCache.TryGetValue(reviewer.Id, out var rc) && rc.ExpiresAt > nowUtc) reviewerEmb = rc.Emb;
                    else if (skillTags.Any())
                    {
                        try { reviewerEmb = await _aiService.GetEmbeddingAsync(string.Join(' ', skillTags)); if (reviewerEmb != null) _reviewerEmbeddingCache[reviewer.Id] = (reviewerEmb, nowUtc.Add(ReviewerEmbeddingCacheTtl)); }
                        catch { }
                    }

                    // Prepare containers
                    var matchedSkills = new List<string>(); var matchedSims = new List<double>();
                    var fieldScores = topicFields.Keys.ToDictionary(k => k, k => 0m, StringComparer.OrdinalIgnoreCase);
                    var topTokens = topicFields.Keys.ToDictionary(k => k, k => new List<string>(), StringComparer.OrdinalIgnoreCase);

                    // Load all skill embeddings for this reviewer first and compute skill<->topic and skill<->field sims
                    // Use precomputed skill embeddings and field sims to determine matched skills quickly
                    var skillEmbMap = new Dictionary<string, float[]>();
                    var skillTopicSimLocal = new Dictionary<string, double>();
                    var skillFieldSimLocal = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);

                    foreach (var s in skillTags)
                    {
                        if (!skillEmbMapGlobal.TryGetValue(s, out var skillEmb)) continue;
                        if (skillEmb == null || topicEmb == null || skillEmb.Length != topicEmb.Length) continue;
                        skillEmbMap[s] = skillEmb;

                        // Ensure we compute per-field sims first so matched-skill decision can require both
                        // (a) strong topic similarity AND (b) at least one field where the skill is relevant.
                        Dictionary<string, double> fld;
                        if (skillFieldSimGlobal.TryGetValue(s, out var gf))
                        {
                            fld = gf;
                        }
                        else
                        {
                            var localFld = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                            foreach (var fk in fieldEmbeddings.Keys)
                            {
                                var fe = fieldEmbeddings[fk];
                                if (fe == null || fe.Length != skillEmb.Length) { localFld[fk] = 0.0; continue; }
                                var sim = _aiService.CosineSimilarity(fe, skillEmb);
                                sim = Math.Max(0.0, Math.Min(1.0, sim));
                                localFld[fk] = sim;
                            }
                            fld = localFld;
                        }

                        // publish per-field maxima into fieldScores
                        skillFieldSimLocal[s] = fld;
                        foreach (var fk in fld.Keys)
                        {
                            var sim = fld[fk];
                            if (sim > (double)fieldScores[fk]) fieldScores[fk] = (decimal)sim;
                        }

                        // topic similarity now; use precomputed if available
                        double simToTopic;
                        if (skillTopicSimGlobal.TryGetValue(s, out var gts)) simToTopic = gts;
                        else { simToTopic = _aiService.CosineSimilarity(topicEmb, skillEmb); simToTopic = Math.Max(0.0, Math.Min(1.0, simToTopic)); }
                        skillTopicSimLocal[s] = simToTopic;

                        // Accept as a matched skill only if it has both strong topic similarity AND at least one
                        // field with meaningful relevance (prevents e.g., blockchain being matched for a music app).
                        var maxFieldSim = fld.Values.DefaultIfEmpty(0.0).Max();
                        if (simToTopic >= SkillTagMatchThreshold && maxFieldSim >= FieldMatchThreshold)
                        {
                            matchedSkills.Add(s);
                            matchedSims.Add(simToTopic);
                        }
                    }

                    // Token-level selection: for each field, iterate candidate n-grams once and accept a token only if
                    // it has high similarity to at least one reviewer skill AND that skill is relevant to that field.
                    foreach (var fk in fieldEmbeddings.Keys)
                    {
                        if (!fieldTokenCandidates.TryGetValue(fk, out var candidates)) continue;
                        var localAdded = new List<(string token, double sim)>();
                        foreach (var (normNg, origNg) in candidates)
                        {
                            var display = origNg;
                            if (!IsMeaningfulToken(display)) continue;
                            if (!_tokenEmbeddingCache.TryGetValue(normNg, out var tokenEmb) || tokenEmb.ExpiresAt <= nowUtc || tokenEmb.Emb == null) continue;

                            double bestTokenSkillSim = 0.0; string? bestSkill = null;
                            foreach (var kvs in skillEmbMap)
                            {
                                var s = kvs.Key; var skEmb = kvs.Value;
                                if (!skillFieldSimLocal.ContainsKey(s) || !skillFieldSimLocal[s].ContainsKey(fk)) continue;
                                var skillToFieldSim = skillFieldSimLocal[s][fk];
                                if (skillToFieldSim < FieldMatchThreshold) continue;
                                if (skEmb.Length != tokenEmb.Emb.Length) continue;
                                var tSim = _aiService.CosineSimilarity(tokenEmb.Emb, skEmb);
                                if (tSim > bestTokenSkillSim) { bestTokenSkillSim = tSim; bestSkill = s; }
                            }

                            // Only accept tokens that are well-explained by a matched skill for this reviewer.
                            // This avoids the same generic tokens appearing for unrelated reviewers.
                            var allow = false;
                            double combinedScore = 0.0;
                            if (bestSkill != null && matchedSkills.Contains(bestSkill, StringComparer.OrdinalIgnoreCase) && bestTokenSkillSim >= TokenMatchThreshold)
                            {
                                var skillTopicSim = skillTopicSimLocal.ContainsKey(bestSkill) ? skillTopicSimLocal[bestSkill] : 0.0;
                                var skillFieldSim = skillFieldSimLocal.ContainsKey(bestSkill) && skillFieldSimLocal[bestSkill].ContainsKey(fk) ? skillFieldSimLocal[bestSkill][fk] : 0.0;
                                // require the explaining skill to also be relevant to the topic (tighten false positives)
                                if (skillTopicSim >= SkillTagMatchThreshold)
                                {
                                    allow = true;
                                    // combined score prioritizes token↔skill, then skill↔field, then skill↔topic
                                    combinedScore = bestTokenSkillSim * skillFieldSim * skillTopicSim;
                                }
                            }

                            // Reviewer-embedding fallback: use *only* when reviewer has NO matched skills (avoid surfacing generic tokens)
                            // and require a much stronger similarity and a multi-word or reasonably long token to reduce false positives.
                            if (!allow && !matchedSkills.Any() && reviewerEmbMap.TryGetValue(reviewer.Id, out var rEmb) && rEmb != null)
                            {
                                try
                                {
                                    var tokenToReviewer = _aiService.CosineSimilarity(tokenEmb.Emb, rEmb);
                                    var tokenIsMultiOrLong = display.Contains(' ') || display.Length >= 6;
                                    if (tokenToReviewer >= 0.95 && tokenIsMultiOrLong)
                                    {
                                        allow = true;
                                        // scale reviewer fallback slightly lower than direct token↔skill evidence
                                        combinedScore = tokenToReviewer * 0.8;
                                    }
                                }
                                catch { }
                            }

                            if (allow && combinedScore > 0.0)
                            {
                                localAdded.Add((display, combinedScore));
                            }
                        }

                        // select top tokens per field for this reviewer, prefer higher combined score then multi-word tokens
                        topTokens[fk] = localAdded
                            .GroupBy(p => p.token, StringComparer.OrdinalIgnoreCase)
                            .Select(g => new { token = g.Key, score = g.Max(x => x.sim) })
                            .OrderByDescending(x => x.score)
                            .ThenByDescending(x => x.token.Count(c => c == ' '))
                            .ThenByDescending(x => x.token.Length)
                            .Take(8)
                            .Select(x => x.token)
                            .ToList();
                    }

                    // Determine skillMatchScore: prefer explicit matched skills; if none, require a reasonably strong
                    // reviewer embedding vs topic embedding to consider a fallback match; otherwise treat as no match.
                    decimal skillMatchScore;
                    if (matchedSims.Any())
                    {
                        skillMatchScore = Decimal.Round((decimal)matchedSims.Max(), 5);
                    }
                    else
                    {
                        var fallback = 0.0;
                        if (topicEmb != null && reviewerEmb != null && topicEmb.Length == reviewerEmb.Length)
                        {
                            fallback = Math.Max(0.0, Math.Min(1.0, _aiService.CosineSimilarity(topicEmb, reviewerEmb)));
                        }
                        // require a stronger fallback to avoid false-positive matches when reviewer has no clear matching skills
                        if (fallback >= 0.50) skillMatchScore = Decimal.Round((decimal)fallback, 5); else skillMatchScore = 0m;
                    }

                    ReviewerPerformance? perf = null; try { if (semesterId.HasValue) perf = reviewer.ReviewerPerformances?.FirstOrDefault(p => p.SemesterId == semesterId.Value); } catch { perf = null; }
                    int currentActiveAssignments = perf != null ? Math.Max(0, perf.TotalAssignments - perf.CompletedAssignments) : (reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) ?? 0);
                    int completedAssignments = perf?.CompletedAssignments ?? reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Completed) ?? 0;
                    decimal performanceScore = perf != null ? (perf.QualityRating ?? 0m) * 0.5m + (perf.OnTimeRate ?? 0m) * 0.3m + (perf.AverageScoreGiven ?? 0m) * 0.2m : CalculatePerformanceScore(reviewer);

                    var workloadScore = 1 - Math.Min(1, currentActiveAssignments / 5m);
                    var overall = skillMatchScore * 0.5m + workloadScore * 0.3m + performanceScore * 0.2m;

                    var dto = new ReviewerSuggestionDTO
                    {
                        ReviewerId = reviewer.Id,
                        ReviewerName = reviewer.Profile?.FullName ?? reviewer.Email ?? "Unknown",
                        ReviewerSkills = skills.ToDictionary(x => x.SkillTag ?? "", x => x.ProficiencyLevel.ToString(), StringComparer.OrdinalIgnoreCase),
                        MatchedSkills = matchedSkills,
                        SkillMatchScore = Decimal.Round(skillMatchScore, 4),
                        SkillMatchFieldScores = fieldScores,
                        SkillMatchTopTokens = topTokens,
                        WorkloadScore = Decimal.Round(workloadScore, 4),
                        PerformanceScore = Decimal.Round(performanceScore, 4),
                        OverallScore = Decimal.Round(overall, 4),
                        CurrentActiveAssignments = currentActiveAssignments,
                        CompletedAssignments = completedAssignments,
                        AverageScoreGiven = perf?.AverageScoreGiven,
                        OnTimeRate = perf?.OnTimeRate,
                        QualityRating = perf?.QualityRating,
                        IsEligible = ((matchedSkills.Any() || skillMatchScore >= (decimal)EligibilityEmbeddingThreshold) && (reviewer.LecturerSkills?.Any() ?? false)),
                        IneligibilityReasons = (matchedSkills.Any() || skillMatchScore >= (decimal)EligibilityEmbeddingThreshold) ? new List<string>() : new List<string> { "No matching skills with topic (semantic similarity below threshold)" }
                    };

                    results.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "CalculateReviewerScores: reviewer {ReviewerId} failed", reviewer?.Id);
                }
            }

            return results;
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
                var sb = new StringBuilder();
                sb.AppendLine("You are an expert reviewer recommender. Use ONLY the provided CANDIDATES and the short submission context below.");
                sb.AppendLine();
                sb.AppendLine("CANDIDATES:");
                foreach (var s in suggestions)
                {
                    // Provide candidate metadata but do NOT expose token phrases to the model here.
                    // We intentionally omit SkillMatchTopTokens so the assistant is forced to explain
                    // relevance in natural language based on the reviewer's skills and scores.
                    var matched = (s.MatchedSkills ?? new List<string>()).Take(4);
                    sb.AppendLine($"Id:{s.ReviewerId} | Name:{s.ReviewerName} | Active:{s.CurrentActiveAssignments} | Overall:{(double)(s.OverallScore * 100):0.0}% | SkillMatch:{(double)(s.SkillMatchScore * 100):0.0}% | Matched:[{string.Join(',', matched)}]");
                }
                sb.AppendLine();
                sb.AppendLine("Submission context (short):");
                sb.AppendLine(submissionContext.Length > 800 ? submissionContext.Substring(0, 800) + "..." : submissionContext);
                sb.AppendLine();
                // Developer note: prompt the model in natural language to (a) pick two primary reviewers,
                // (b) for each recommended reviewer explain which reviewer skills make them appropriate for
                //     the submission (reference up to 2 matched skills and up to 2 top tokens), and
                // (c) list up to two backups. Keep output short, factual and evidence-based. This comment helps
                // future maintainers understand the NLP intent; the following text is what is sent to the model.
                sb.AppendLine("Task for the assistant: read only the CANDIDATES and the short submission context provided above.");
                sb.AppendLine("Write exactly two paragraphs separated by a single blank line. Use plain natural language only (no JSON, lists, tables, or code fences). Keep the English paragraph concise but informative (about 2-5 short sentences).");
                sb.AppendLine("Paragraph 1 (English): Start with 'Based on the submission context, I recommend' followed by the two primary reviewers in this format: FullName (ReviewerId: 12345). For each recommended reviewer include 'Overall: X.X%', 'SkillMatch: Y.Y%', and 'Active: N' (current active assignments). Give one short evidence sentence per reviewer explaining, in natural language, which of their listed skills make them appropriate for this submission — cite up to two matched skills. DO NOT repeat or list token phrases; instead explain why the skill is relevant (for example: 'because they have experience building distributed ledger systems that align with the project's decentralization goals'). Prefer reviewers with fewer active assignments when choosing primaries. Finish with a short sentence naming up to two backup reviewers in the format: Backups: Name (ReviewerId) and Name (ReviewerId).");
                sb.AppendLine("Paragraph 2 (Vietnamese): Provide a faithful, concise Vietnamese translation of paragraph 1; keep reviewer names and ids identical.");
                sb.AppendLine("Rules: do not add headings, extra commentary, JSON, or code fences. Use only the candidate data shown above. If fewer than two primaries exist, list only the available reviewer(s). Keep language factual and focused on evidence.");
                sb.AppendLine("If two reviewers show similar top tokens or seemingly overlapping terminology, add one short sentence explaining why their skills can be related (e.g., integration points between domains, shared terminology, or that a token is generic). Again, do NOT print token phrases — explain the relationship in natural language.");

                var aiResp = await _aiService.GetPromptCompletionAsync(sb.ToString());
                if (string.IsNullOrWhiteSpace(aiResp)) return BuildDeterministicFallback(suggestions);

                var res = aiResp.Trim();
                // strip code fences if present
                if (res.StartsWith("```") && res.EndsWith("```"))
                {
                    var lines = res.Split('\n');
                    var start = (lines.Length > 0 && lines[0].StartsWith("```")) ? 1 : 0;
                    var end = (lines.Length > 1 && lines[lines.Length - 1].StartsWith("```")) ? lines.Length - 1 : lines.Length;
                    res = string.Join('\n', lines.Skip(start).Take(Math.Max(0, end - start))).Trim();
                }

                // enforce strict two-paragraph format and starting phrase
                if (!res.Contains("\n\n") || !res.TrimStart().StartsWith("Based on the submission context, I recommend", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("AI explanation did not match required format — using deterministic fallback.");
                    return BuildDeterministicFallback(suggestions);
                }

                return res;
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
            var primaries = ordered.Take(2).ToList();
            var backups = ordered.Skip(2).Take(2).ToList();

            string primaryPart;
            if (primaries.Count == 2)
            {
                primaryPart = $"{primaries[0].ReviewerName} (ReviewerId: {primaries[0].ReviewerId}) and {primaries[1].ReviewerName} (ReviewerId: {primaries[1].ReviewerId})";
            }
            else if (primaries.Count == 1)
            {
                primaryPart = $"{primaries[0].ReviewerName} (ReviewerId: {primaries[0].ReviewerId})";
            }
            else
            {
                primaryPart = "no suitable primary reviewers found";
            }

            string backupPart;
            if (backups.Count >= 2)
            {
                backupPart = $"{backups[0].ReviewerName} (ReviewerId: {backups[0].ReviewerId}) and {backups[1].ReviewerName} (ReviewerId: {backups[1].ReviewerId})";
            }
            else if (backups.Count == 1)
            {
                backupPart = $"{backups[0].ReviewerName} (ReviewerId: {backups[0].ReviewerId})";
            }
            else
            {
                backupPart = "no suitable backups available";
            }

            // compact reason: combine top matched skills across primaries
            var topSkills = primaries.SelectMany(p => p.MatchedSkills ?? Enumerable.Empty<string>()).GroupBy(s => s).OrderByDescending(g => g.Count()).Select(g => g.Key).Take(3).ToList();
            var reason = topSkills.Any() ? $"they have expertise in {string.Join(", ", topSkills)}" : "their relevant expertise and history supervising related projects";

            var en = new StringBuilder();
            if (errorNote) en.Append("(Automatic fallback used due to AI error) ");
            en.Append($"Based on the submission context, I recommend {primaryPart} as primary reviewers because {reason}. For backups, consider {backupPart}.");

            // Simple Vietnamese translation preserving names and ids
            var vn = new StringBuilder();
            if (errorNote) vn.Append("(Sử dụng phương án dự phòng do lỗi AI) ");
            vn.Append($"Dựa trên nội dung đề tài, tôi đề xuất {primaryPart} làm phản biện chính vì {reason}. Để dự phòng, cân nhắc {backupPart}.");

            return en.ToString().Trim() + "\n\n" + vn.ToString().Trim();
        }
        
        public async Task<BaseResponseModel<ReviewerSuggestionOutputDTO>> SuggestReviewersAsync(ReviewerSuggestionInputDTO input)
        {
            if (input == null) return new BaseResponseModel<ReviewerSuggestionOutputDTO> { IsSuccess = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Input is null" };
            var tv = await _unitOfWork.GetRepo<TopicVersion>().GetSingleAsync(new QueryOptions<TopicVersion> { Predicate = t => t.Id == input.TopicVersionId, IncludeProperties = new List<Expression<Func<TopicVersion, object>>> { t => t.Topic } });
            if (tv == null || tv.Topic == null) return new BaseResponseModel<ReviewerSuggestionOutputDTO> { IsSuccess = false, StatusCode = StatusCodes.Status404NotFound, Message = "Topic version not found." };
            return await SuggestReviewersByTopicIdAsync(new ReviewerSuggestionByTopicInputDTO { TopicId = tv.TopicId, MaxSuggestions = input.MaxSuggestions, UsePrompt = input.UsePrompt });
        }

        public async Task<BaseResponseModel<ReviewerSuggestionOutputDTO>> SuggestReviewersByTopicIdAsync(ReviewerSuggestionByTopicInputDTO input)
        {
            // Reuse the previous logic by loading topic version via TopicId -> Topic (simpler path)
            try
            {
                var topic = await _unitOfWork.GetRepo<Topic>().GetSingleAsync(new QueryOptions<Topic> { Predicate = t => t.Id == input.TopicId, IncludeProperties = new List<Expression<Func<Topic, object>>> { t => t.Category } });
                if (topic == null || topic.Category == null)
                {
                    return new BaseResponseModel<ReviewerSuggestionOutputDTO> { IsSuccess = false, StatusCode = StatusCodes.Status404NotFound, Message = "Topic not found." };
                }

                var topicContext = string.Join(" ", new[]
                {
                    topic.Category.Name,
                    topic.EN_Title,
                    topic.VN_title,
                    topic.Description,
                    topic.Objectives,
                    topic.Problem,
                    topic.Content,
                    topic.Context
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

                if (string.IsNullOrWhiteSpace(topicContext)) return new BaseResponseModel<ReviewerSuggestionOutputDTO> { IsSuccess = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Topic context empty." };

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

                if (!reviewers.Any()) return new BaseResponseModel<ReviewerSuggestionOutputDTO> { IsSuccess = false, StatusCode = StatusCodes.Status404NotFound, Message = "No eligible reviewers found." };

                var topicFields = new Dictionary<string, string>
                {
                    { "Title", topic.EN_Title ?? string.Empty },
                    { "Category", topic.Category?.Name ?? string.Empty },
                    { "Description", topic.Description ?? string.Empty },
                    { "Objectives", topic.Objectives ?? string.Empty },
                    { "Content", topic.Content ?? string.Empty },
                    { "Context", topic.Context ?? string.Empty }
                };

                foreach (var k in topicFields.Keys.ToList())
                {
                    var v = topicFields[k] ?? string.Empty;
                    if (v.Length < 10)
                    {
                        var extra = topicContext.Length > 200 ? topicContext.Substring(0, 200) : topicContext;
                        topicFields[k] = (v + " " + extra).Trim();
                    }
                }

                var skipMessages = new List<string>();
                int? semesterId = topic.SemesterId;
                var reviewerScores = await CalculateReviewerScores(reviewers, topicFields, topicContext, skipMessages, semesterId);

                var max = input.MaxSuggestions <= 0 ? 5 : input.MaxSuggestions;

                var suggestions = reviewerScores
                    .OrderBy(r => r.CurrentActiveAssignments)
                    .ThenByDescending(r => r.OverallScore)
                    .Take(max)
                    .ToList();

                string? aiExplanation = null;
                if (input.UsePrompt)
                {
                    try
                    {
                        aiExplanation = await GenerateAIExplanation(topicContext, suggestions);
                    }
                    catch (App.BLL.Services.AIQuotaExceededException qex)
                    {
                        _logger.LogError(qex, "AI quota exceeded while generating explanation for TopicId {TopicId}", input.TopicId);
                        skipMessages.Add("AI provider quota exceeded; stopping AI calls. Administrator intervention required.");
                        try { _appLifetime.StopApplication(); } catch { }
                        aiExplanation = "AI quota exceeded; explanation unavailable.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "AI explanation generation failed for TopicId {TopicId}", input.TopicId);
                        aiExplanation = "Failed to generate AI explanation: a provider error occurred.";
                    }
                }

                return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                {
                    Data = new ReviewerSuggestionOutputDTO { Suggestions = suggestions, AIExplanation = aiExplanation, SkipMessages = skipMessages },
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Reviewer suggestions generated successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SuggestReviewersByTopicIdAsync failed");
                return new BaseResponseModel<ReviewerSuggestionOutputDTO> { IsSuccess = false, StatusCode = StatusCodes.Status500InternalServerError, Message = "An unexpected error occurred while generating suggestions." };
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

            if (reviewer == null) return new BaseResponseModel<ReviewerEligibilityDTO> { IsSuccess = false, StatusCode = StatusCodes.Status404NotFound, Message = "Reviewer not found." };

            var topicVersion = await _unitOfWork.GetRepo<TopicVersion>().GetSingleAsync(
                new QueryOptions<TopicVersion>
                {
                    Predicate = tv => tv.Id == topicVersionId,
                    IncludeProperties = new List<Expression<Func<TopicVersion, object>>> { tv => tv.Topic }
                });

            if (topicVersion == null) return new BaseResponseModel<ReviewerEligibilityDTO> { IsSuccess = false, StatusCode = StatusCodes.Status404NotFound, Message = "Topic version not found." };

            var reasons = new List<string>();
            var activeAssignments = reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) ?? 0;
            if (activeAssignments >= 5) reasons.Add("Reviewer has too many active assignments.");
            var hasSkills = reviewer.LecturerSkills.Any(); if (!hasSkills) reasons.Add("Reviewer has no recorded skills.");
            var isEligible = !reasons.Any();

            return new BaseResponseModel<ReviewerEligibilityDTO>
            {
                Data = new ReviewerEligibilityDTO { ReviewerId = reviewer.Id, TopicVersionId = topicVersionId, TopicId = topicVersion.TopicId, IsEligible = isEligible, IneligibilityReasons = isEligible ? new List<string>() : reasons },
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

            if (reviewer == null) return new BaseResponseModel<ReviewerEligibilityDTO> { IsSuccess = false, StatusCode = StatusCodes.Status404NotFound, Message = "Reviewer not found." };

            var topic = await _unitOfWork.GetRepo<Topic>().GetSingleAsync(new QueryOptions<Topic> { Predicate = t => t.Id == topicId });
            if (topic == null) return new BaseResponseModel<ReviewerEligibilityDTO> { IsSuccess = false, StatusCode = StatusCodes.Status404NotFound, Message = "Topic not found." };

            var reasons = new List<string>();
            var activeAssignments = reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) ?? 0;
            if (activeAssignments >= 5) reasons.Add("Reviewer has too many active assignments.");
            var hasSkills = reviewer.LecturerSkills.Any(); if (!hasSkills) reasons.Add("Reviewer has no recorded skills.");
            var isEligible = !reasons.Any();

            return new BaseResponseModel<ReviewerEligibilityDTO>
            {
                Data = new ReviewerEligibilityDTO { ReviewerId = reviewer.Id, TopicId = topicId, IsEligible = isEligible, IneligibilityReasons = isEligible ? new List<string>() : reasons },
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Eligibility check completed successfully."
            };
        }
    }
}