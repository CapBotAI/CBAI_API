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
        // Cache for reviewer embeddings (semantic vectors) to avoid repeated external calls
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, (float[] Emb, DateTime ExpiresAt)> _reviewerEmbeddingCache = new();
        private static readonly TimeSpan ReviewerEmbeddingCacheTtl = TimeSpan.FromHours(1);
        // Cache for individual skill-tag embeddings (to support per-skill semantic matching)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (float[] Emb, DateTime ExpiresAt)> _skillEmbeddingCache = new();
        private static readonly TimeSpan SkillEmbeddingCacheTtl = TimeSpan.FromDays(7);
    // Cache for topic field embeddings (Title, Description, etc.)
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (float[] Emb, DateTime ExpiresAt)> _fieldEmbeddingCache = new();
    private static readonly TimeSpan FieldEmbeddingCacheTtl = TimeSpan.FromDays(7);
    // Cache for small token embeddings used to pick top tokens per field
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (float[] Emb, DateTime ExpiresAt)> _tokenEmbeddingCache = new();
    private static readonly TimeSpan TokenEmbeddingCacheTtl = TimeSpan.FromDays(30);
    private const double TokenMatchThreshold = 0.45; // token-to-skill similarity threshold to show as top token (raised to reduce noise)
    private const double FieldMatchThreshold = 0.30; // per-field similarity required to consider a skill as truly matched to a field
    // Matching thresholds (tuneable)
    private const double SkillTagMatchThreshold = 0.60; // per-skill embedding similarity required to consider a tag matched (raised for stability)
    private const double EligibilityEmbeddingThreshold = 0.25; // overall embedding threshold to mark reviewer eligible when no literal token overlap

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
            if (string.IsNullOrWhiteSpace(submissionContext)) throw new ArgumentException("Submission context cannot be null or empty.", nameof(submissionContext));

            var reviewerScores = new List<ReviewerSuggestionDTO>();

            // Prepare a topic embedding once (best-effort)
                float[]? topicEmbedding = null;
                // Topic embedding cache (keyed by full submissionContext)
                try
                {
                    var topicCacheKey = "topic|" + submissionContext;
                    if (_fieldEmbeddingCache.TryGetValue(topicCacheKey, out var te) && te.ExpiresAt > DateTime.UtcNow)
                    {
                        topicEmbedding = te.Emb;
                    }
                    else
                    {
                        var emb = await _aiService.GetEmbeddingAsync(submissionContext);
                        if (emb != null && emb.Length > 0)
                        {
                            topicEmbedding = emb;
                            _fieldEmbeddingCache[topicCacheKey] = (emb, DateTime.UtcNow.Add(FieldEmbeddingCacheTtl));
                        }
                        else
                        {
                            // If provider returned null (service unavailable), try to use any existing cached embedding
                            if (te.Emb != null) topicEmbedding = te.Emb;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Topic embedding failed - embeddings-only mode will mark semantic scores as 0");
                }

            foreach (var reviewer in reviewers)
            {
                try
                {
                    // skip overloaded reviewers
                    var activeCount = reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) ?? 0;
                    if (activeCount >= 5)
                    {
                        var msg = $"Reviewer {reviewer.Id} skipped because current active assignments ({activeCount}) >= 5";
                        _logger.LogDebug(msg);
                        skipMessages?.Add(msg);
                        continue;
                    }

                    var skills = reviewer.LecturerSkills ?? Enumerable.Empty<LecturerSkill>();
                    if (!skills.Any()) continue;

                    var reviewerSkillDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var s in skills)
                    {
                        if (string.IsNullOrWhiteSpace(s.SkillTag)) continue;
                        reviewerSkillDict[s.SkillTag.Trim()] = s.ProficiencyLevel.ToString();
                    }

                    // Get or compute reviewer-wide embedding
                    float[]? reviewerEmb = null;
                    var nowUtc = DateTime.UtcNow;
                    try
                    {
                        if (_reviewerEmbeddingCache.TryGetValue(reviewer.Id, out var re) && re.ExpiresAt > nowUtc)
                        {
                            reviewerEmb = re.Emb;
                        }
                        else
                        {
                            var skillText = string.Join(" ", reviewerSkillDict.Keys);
                            if (!string.IsNullOrWhiteSpace(skillText))
                            {
                                reviewerEmb = await _aiService.GetEmbeddingAsync(skillText);
                                if (reviewerEmb != null && reviewerEmb.Length > 0) _reviewerEmbeddingCache[reviewer.Id] = (reviewerEmb, nowUtc.Add(ReviewerEmbeddingCacheTtl));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Reviewer-wide embedding failed for {ReviewerId}", reviewer.Id);
                        reviewerEmb = null;
                    }

                    var matchedSkills = new List<string>();
                    var matchedSkillSims = new List<double>();
                    double chosenSemantic = 0.0;

                    if (topicEmbedding != null)
                    {
                        try
                        {
                            double maxSim = 0.0;
                            var now = DateTime.UtcNow;

                            // Prepare per-field max similarity map (will be filled by comparing skill embeddings vs each topic field)
                            var fieldMaxSim = topicFields.ToDictionary(k => k.Key, v => 0.0);

                            foreach (var s in skills)
                            {
                                var tag = s.SkillTag?.Trim();
                                if (string.IsNullOrWhiteSpace(tag)) continue;

                                float[]? skillEmb = null;
                                bool skillCached = false;
                                if (_skillEmbeddingCache.TryGetValue(tag, out var se) && se.ExpiresAt > now)
                                {
                                    skillEmb = se.Emb;
                                    skillCached = true;
                                }
                                else
                                {
                                    try
                                    {
                                        skillEmb = await _aiService.GetEmbeddingAsync(tag);
                                        if (skillEmb != null && skillEmb.Length > 0) _skillEmbeddingCache[tag] = (skillEmb, now.Add(SkillEmbeddingCacheTtl));
                                    }
                                    catch (Exception innerEx)
                                    {
                                        _logger.LogDebug(innerEx, "Skill embedding failed for tag '{Tag}'", tag);
                                        skillEmb = null;
                                    }
                                }

                                if (skillEmb == null || topicEmbedding == null || skillEmb.Length != topicEmbedding.Length)
                                {
                                    _logger.LogDebug("Reviewer {ReviewerId} skill '{Tag}' skipped: skillEmb null? {IsNull} topicEmb null? {TopicNull} lengths equal? {LenEqual}", reviewer.Id, tag, skillEmb == null, topicEmbedding == null, skillEmb != null && topicEmbedding != null ? skillEmb.Length == topicEmbedding.Length : false);
                                    continue;
                                }

                                // similarity between overall topic and skill
                                var sim = _aiService.CosineSimilarity(topicEmbedding, skillEmb);
                                sim = Math.Max(0.0, Math.Min(1.0, sim));

                                _logger.LogDebug("Reviewer {ReviewerId} skill '{Tag}' sim={Sim:0.0000} cachedSkillEmb={Cached} skillLen={SkillLen} topicLen={TopicLen}", reviewer.Id, tag, sim, skillCached, skillEmb?.Length ?? 0, topicEmbedding?.Length ?? 0);

                                // track this skill's max similarity against any single topic field
                                double skillMaxFieldSim = 0.0;

                                if (sim > maxSim) maxSim = sim;

                                // update per-field similarities: compare each topic field embedding to this skill embedding
                                foreach (var fieldKvp in topicFields)
                                {
                                    var fieldKey = fieldKvp.Key;
                                    var fieldText = fieldKvp.Value;
                                    if (string.IsNullOrWhiteSpace(fieldText)) continue;

                                    // try get cached field embedding
                                    float[]? fieldEmb = null;
                                    if (_fieldEmbeddingCache.TryGetValue(fieldKey + "|" + fieldText, out var fe) && fe.ExpiresAt > DateTime.UtcNow)
                                    {
                                        fieldEmb = fe.Emb;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            fieldEmb = await _aiService.GetEmbeddingAsync(fieldText);
                                            if (fieldEmb != null && fieldEmb.Length > 0) _fieldEmbeddingCache[fieldKey + "|" + fieldText] = (fieldEmb, DateTime.UtcNow.Add(FieldEmbeddingCacheTtl));
                                        }
                                        catch (Exception innerEx)
                                        {
                                            _logger.LogDebug(innerEx, "Field embedding failed for field '{FieldKey}'", fieldKey);
                                            fieldEmb = null;
                                        }
                                    }

                                        if (fieldEmb == null || skillEmb == null || fieldEmb.Length != skillEmb.Length) continue;

                                        try
                                        {
                                            var fSim = _aiService.CosineSimilarity(fieldEmb, skillEmb);
                                            fSim = Math.Max(0.0, Math.Min(1.0, fSim));
                                            if (fSim > fieldMaxSim[fieldKey]) fieldMaxSim[fieldKey] = fSim;
                                            if (fSim > skillMaxFieldSim) skillMaxFieldSim = fSim;
                                        }
                                        catch { }
                                }

                                
                                // If this skill both matches the overall topic (semantic) and has at least one
                                // strongly related topic field, mark it as a matched skill. This prevents
                                // unrelated reviewers (e.g. blockchain, marketing) from being considered matched
                                // when their tag-topic sim is incidental.
                                try
                                {
                                    if (sim >= SkillTagMatchThreshold && skillMaxFieldSim >= FieldMatchThreshold)
                                    {
                                        matchedSkills.Add(tag);
                                        matchedSkillSims.Add(sim);
                                    }
                                }
                                catch { }
                            }

                            if (reviewerEmb != null && topicEmbedding != null && reviewerEmb.Length == topicEmbedding.Length)
                            {
                                try
                                {
                                    var rSim = _aiService.CosineSimilarity(topicEmbedding, reviewerEmb);
                                    rSim = Math.Max(0.0, Math.Min(1.0, rSim));
                                    if (rSim > maxSim) maxSim = rSim;
                                }
                                catch { }
                            }

                            chosenSemantic = maxSim;

                            // Debug log: per-reviewer semantic breakdown
                            try
                            {
                                _logger.LogDebug("Reviewer {ReviewerId} semantic breakdown: maxSim={MaxSim:0.0000} matchedSkills=[{Matched}]",
                                    reviewer.Id, maxSim, string.Join(',', matchedSkills));
                            }
                            catch { }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Reviewer per-skill semantic match failed for {ReviewerId}", reviewer.Id);
                            chosenSemantic = 0.0;
                        }
                    }

                            // Determine the skill match score: prefer the strongest matched-skill similarity
                            // when we actually have matched skills; otherwise fall back to the reviewer/topic
                            // semantic similarity (chosenSemantic).
                            decimal skillMatchScore = matchedSkillSims.Any()
                                ? Decimal.Round((decimal)matchedSkillSims.Max(), 4)
                                : Decimal.Round((decimal)Math.Max(0.0, Math.Min(1.0, chosenSemantic)), 4);

                    // Field scores: use fieldMaxSim computed during per-skill loop
                    var fieldScores = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                    var fieldTopTokens = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                    foreach (var key in topicFields.Keys)
                    {
                        // fieldMaxSim may not exist if topicEmbedding was null or skills loop skipped; default 0
                        fieldScores[key] = 0m;
                        fieldTopTokens[key] = new List<string>(); // will be filled later per-reviewer
                    }

                    // If topicEmbedding was available, and we computed per-field maxima, try to use those replacements now
                    if (topicEmbedding != null)
                    {
                        // Recompute per-field maxima by comparing each field embedding to the reviewer's skill embeddings (cheap because embeddings cached above)
                        try
                        {
                            var now = DateTime.UtcNow;
                            // ensure we have at least one skill embedding for this reviewer
                            var reviewerSkillEmbeddings = new List<float[]>();
                            foreach (var s in skills)
                            {
                                var tag = s.SkillTag?.Trim();
                                if (string.IsNullOrWhiteSpace(tag)) continue;
                                if (_skillEmbeddingCache.TryGetValue(tag, out var se) && se.ExpiresAt > now)
                                {
                                    if (se.Emb != null) reviewerSkillEmbeddings.Add(se.Emb);
                                }
                            }

                            if (reviewerSkillEmbeddings.Count > 0)
                            {
                                foreach (var fk in topicFields.Keys.ToList())
                                {
                                    var fieldText = topicFields[fk];
                                    if (string.IsNullOrWhiteSpace(fieldText)) { fieldScores[fk] = 0m; continue; }

                                    float[]? fieldEmb = null;
                                    if (_fieldEmbeddingCache.TryGetValue(fk + "|" + fieldText, out var fe) && fe.ExpiresAt > DateTime.UtcNow)
                                    {
                                        fieldEmb = fe.Emb;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            fieldEmb = await _aiService.GetEmbeddingAsync(fieldText);
                                            if (fieldEmb != null && fieldEmb.Length > 0) _fieldEmbeddingCache[fk + "|" + fieldText] = (fieldEmb, DateTime.UtcNow.Add(FieldEmbeddingCacheTtl));
                                        }
                                        catch { fieldEmb = null; }
                                    }

                                    if (fieldEmb == null) { fieldScores[fk] = 0m; continue; }

                                    double fMax = 0.0;
                                    foreach (var sEmb in reviewerSkillEmbeddings)
                                    {
                                        if (sEmb == null || sEmb.Length != fieldEmb.Length) continue;
                                        try
                                        {
                                            var fSim = _aiService.CosineSimilarity(fieldEmb, sEmb);
                                            fSim = Math.Max(0.0, Math.Min(1.0, fSim));
                                            if (fSim > fMax) fMax = fSim;
                                        }
                                        catch { }
                                    }

                                    fieldScores[fk] = Decimal.Round((decimal)fMax, 4);

                                    // Now compute top tokens for this field specific to this reviewer: choose tokens most similar to any reviewer skill embedding
                                    try
                                    {
                                        var tokens = new List<(string token, double sim)>();
                                        var cleaned = new string(fieldText.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
                                        var parts = cleaned.Split(new[] { ' ', '\t', '\n', '\r', ',', '.', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Where(p => p.Length > 2).Select(p => p.Trim()).Distinct().Take(12).ToList();

                                        foreach (var token in parts)
                                        {
                                            try
                                            {
                                                float[]? tokenEmb = null;
                                                var tKey = "token|" + token;
                                                if (_tokenEmbeddingCache.TryGetValue(tKey, out var te) && te.ExpiresAt > DateTime.UtcNow)
                                                {
                                                    tokenEmb = te.Emb;
                                                }
                                                else
                                                {
                                                    tokenEmb = await _aiService.GetEmbeddingAsync(token);
                                                    if (tokenEmb != null && tokenEmb.Length > 0) _tokenEmbeddingCache[tKey] = (tokenEmb, DateTime.UtcNow.Add(TokenEmbeddingCacheTtl));
                                                }

                                                if (tokenEmb == null) continue;
                                                double tokenMaxSim = 0.0;
                                                foreach (var sEmb in reviewerSkillEmbeddings)
                                                {
                                                    if (sEmb == null || sEmb.Length != tokenEmb.Length) continue;
                                                    var tSim = _aiService.CosineSimilarity(tokenEmb, sEmb);
                                                    tSim = Math.Max(0.0, Math.Min(1.0, tSim));
                                                    if (tSim > tokenMaxSim) tokenMaxSim = tSim;
                                                }

                                                tokens.Add((token, tokenMaxSim));
                                            }
                                            catch { }
                                        }

                                        var best = tokens.OrderByDescending(t => t.sim).Where(t => t.sim >= TokenMatchThreshold).Take(3).Select(t => t.token).ToList();
                                        fieldTopTokens[fk] = best;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogDebug(ex, "Token top-token extraction failed for reviewer {ReviewerId} field {Field}", reviewer.Id, fk);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Per-field similarity computation failed for reviewer {ReviewerId}", reviewer.Id);
                        }
                    }

                    // Performance metrics (same as before)
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
                        var active = perf.TotalAssignments - perf.CompletedAssignments;
                        currentActiveAssignments = Math.Max(0, active);
                        completedAssignments = perf.CompletedAssignments;
                        var quality = perf.QualityRating ?? 0m;
                        var onTime = perf.OnTimeRate ?? 0m;
                        var avgScore = perf.AverageScoreGiven ?? 0m;
                        performanceScore = quality * 0.5m + onTime * 0.3m + avgScore * 0.2m;
                    }
                    else
                    {
                        currentActiveAssignments = reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) ?? 0;
                        completedAssignments = reviewer.ReviewerAssignments?.Count(a => a.Status == AssignmentStatus.Completed) ?? 0;
                        performanceScore = CalculatePerformanceScore(reviewer);
                    }

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
                        AverageScoreGiven = perf?.AverageScoreGiven,
                        OnTimeRate = perf?.OnTimeRate,
                        QualityRating = perf?.QualityRating,
                        IsEligible = ((matchedSkills.Any() || skillMatchScore >= (decimal)EligibilityEmbeddingThreshold) && (reviewer.LecturerSkills?.Any() ?? false)),
                        IneligibilityReasons = (matchedSkills.Any() || skillMatchScore >= (decimal)EligibilityEmbeddingThreshold) ? new List<string>() : new List<string> { "No matching skills with topic (semantic similarity below threshold)" }
                    };

                    _logger.LogDebug("Reviewer {ReviewerId}: skillScore={SkillScore} matched={Matched}", reviewer.Id, dto.SkillMatchScore, string.Join(',', dto.MatchedSkills));
                        try
                        {
                            _logger.LogDebug("Reviewer {ReviewerId} field scores: {FieldScores}", reviewer.Id, string.Join(", ", dto.SkillMatchFieldScores.Select(kv => kv.Key + ":" + kv.Value)));
                        }
                        catch { }
                    reviewerScores.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to calculate embedding-driven score for reviewer {ReviewerId}.", reviewer.Id);
                }
            }

            return reviewerScores;
        }

        // NOTE: Tokenization, TF and synonym helpers removed - this service now uses embeddings-only matching.

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