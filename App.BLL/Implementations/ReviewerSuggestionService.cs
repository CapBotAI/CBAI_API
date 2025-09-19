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
using App.BLL.Services; // For GeminiAIService and VectorMath
using Microsoft.Extensions.Logging;

namespace App.BLL.Implementations
{
    public class ReviewerSuggestionService : IReviewerSuggestionService
    {
        private readonly GeminiAIService _aiService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReviewerSuggestionService> _logger;

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
                    reviewerScores = await CalculateReviewerScores(reviewers, submissionContext);
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

        private async Task<List<ReviewerSuggestionDTO>> CalculateReviewerScores(List<User> reviewers, string submissionContext)
        {
            // Validate submission context
            if (string.IsNullOrWhiteSpace(submissionContext))
            {
                throw new ArgumentException("Submission context cannot be null or empty.", nameof(submissionContext));
            }

            // Log the submission context for debugging
            _logger.LogDebug("Generating embedding for submission context: {SubmissionContext}", submissionContext);

            // Get topic embedding
            float[] topicEmbedding;
            try
            {
                topicEmbedding = await _aiService.GetEmbeddingAsync(submissionContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for submission context. SubmissionContext: {SubmissionContext}", submissionContext);
                throw new InvalidOperationException("Failed to generate embedding for submission context.", ex);
            }

            var reviewerScores = new List<ReviewerSuggestionDTO>();

            foreach (var reviewer in reviewers)
            {
                try
                {
                    // Step 1: Validate and calculate skill match score
                    var skillText = string.Join(", ", reviewer.LecturerSkills.Select(s => $"{s.SkillTag} ({s.ProficiencyLevel})"));
                    if (string.IsNullOrWhiteSpace(skillText))
                    {
                        _logger.LogDebug("Reviewer {ReviewerId} has no valid skills.", reviewer.Id);
                        continue;
                    }

                    float[] reviewerEmbedding;
                    try
                    {
                        reviewerEmbedding = await _aiService.GetEmbeddingAsync(skillText);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error generating embedding for reviewer {ReviewerId}. Skipping reviewer.", reviewer.Id);
                        continue;
                    }

                    var skillMatchScore = VectorMath.CosineSimilarity(topicEmbedding, reviewerEmbedding);

                    // Step 2: Calculate workload score
                    var workloadScore = CalculateWorkloadScore(reviewer);

                    // Step 3: Calculate performance score
                    var performanceScore = CalculatePerformanceScore(reviewer);

                    // Step 4: Combine scores into overall score
                    reviewerScores.Add(new ReviewerSuggestionDTO
                    {
                        ReviewerId = reviewer.Id,
                        ReviewerName = reviewer.Email ?? "Unknown",
                        SkillMatchScore = skillMatchScore,
                        WorkloadScore = workloadScore,
                        PerformanceScore = performanceScore,
                        OverallScore = skillMatchScore * 0.5m + workloadScore * 0.3m + performanceScore * 0.2m,
                        CurrentActiveAssignments = reviewer.ReviewerAssignments.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress)
                    });
                }
                catch (Exception ex)
                {
                    // Log and skip reviewers with issues
                    _logger.LogWarning(ex, "Failed to calculate score for reviewer {ReviewerId}.", reviewer.Id);
                }
            }

            return reviewerScores;
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
                // Build a compact prompt and guard its length to avoid exceeding model input limits
                var reviewersText = string.Join("\n", suggestions.Select((s, i) => $"Reviewer {i + 1}: {s.ReviewerName}, Score: {s.OverallScore:F2}"));
                var basePrompt = $"Given the submission context:\n---\n{submissionContext}\n---\nand these reviewer profiles:\n{reviewersText}\nExplain why these reviewers are suitable.";

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

        public Task<BaseResponseModel<ReviewerSuggestionOutputDTO>> SuggestReviewersAsync(ReviewerSuggestionInputDTO input)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponseModel<ReviewerEligibilityDTO>> CheckReviewerEligibilityAsync(int reviewerId, int submissionId)
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

            // Perform eligibility checks (e.g., skills, workload, performance)
            var isEligible = reviewer.LecturerSkills.Any() && reviewer.ReviewerAssignments.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) < 5;

            return new BaseResponseModel<ReviewerEligibilityDTO>
            {
                Data = new ReviewerEligibilityDTO
                {
                    ReviewerId = reviewer.Id,
                    IsEligible = isEligible,
                    IneligibilityReasons = isEligible ? new List<string>() : new List<string> { "Reviewer has too many active assignments." }
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

            // Perform eligibility checks (e.g., skills, workload, performance)
            var hasRelevantSkills = reviewer.LecturerSkills.Any(s => s.SkillTag.Contains("relevant keyword", StringComparison.OrdinalIgnoreCase));
            var isEligible = hasRelevantSkills && reviewer.ReviewerAssignments.Count(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) < 5;

            return new BaseResponseModel<ReviewerEligibilityDTO>
            {
                Data = new ReviewerEligibilityDTO
                {
                    ReviewerId = reviewer.Id,
                    IsEligible = isEligible,
                    IneligibilityReasons = isEligible ? new List<string>() : new List<string> { "Reviewer has too many active assignments or lacks relevant skills." }
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
            var reviewerScores = await CalculateReviewerScores(reviewers, topicContext);

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