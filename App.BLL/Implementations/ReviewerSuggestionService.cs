using App.BLL.Interfaces;
using App.BLL.Mapping;
using App.BLL.Services;
using App.Commons.ResponseModel;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.ReviewerSuggestion;
using App.Entities.Entities.App;
using App.Entities.Entities.Core;
using App.Entities.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.BLL.Implementations
{
    /// <summary>
    /// Reviewer suggestion logic using Gemini API, optimized by prioritizing less-busy reviewers.
    /// </summary>
    public class ReviewerSuggestionService : IReviewerSuggestionService
    {
        private readonly GeminiAIService _aiService;
        private readonly IUnitOfWork _unitOfWork;

        public ReviewerSuggestionService(GeminiAIService aiService, IUnitOfWork unitOfWork)
        {
            _aiService = aiService;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseModel<ReviewerSuggestionOutputDTO>> SuggestReviewersAsync(ReviewerSuggestionInputDTO input)
        {
            // 1. Get TopicVersion (with Topic & Category)
            var topicVersionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var topicVersion = await topicVersionRepo.GetSingleAsync(
                new QueryBuilder<TopicVersion>()
                    .WithPredicate(tv => tv.Id == input.TopicVersionId)
                    .WithInclude(tv => tv.Topic)
                    .WithInclude(tv => tv.Topic.Category)
                    .Build()
            );
            if (topicVersion == null)
                return new BaseResponseModel<ReviewerSuggestionOutputDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Topic version not found"
                };

            var topic = topicVersion.Topic;
            var topicCategory = topic?.Category;
            var topicContext = $"{topicCategory?.Name ?? ""} {topic?.Title ?? ""} {topicVersion.Title ?? ""} {topicVersion.Description ?? ""} {topicVersion.Objectives ?? ""} {topicVersion.Methodology ?? ""} {topicVersion.ExpectedOutcomes ?? ""}";

            // 2. Get reviewers: Users with Reviewer role and at least one LecturerSkill
            var userRepo = _unitOfWork.GetRepo<User>();
            var allUsers = await userRepo.GetAllAsync(
                new QueryBuilder<User>()
                    .WithInclude(u => u.LecturerSkills)
                    .WithInclude(u => u.UserRoles)
                    .Build()
            );
            var reviewers = allUsers
                .Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Reviewer") && u.LecturerSkills.Any())
                .ToList();

            // 3. Get ReviewerPerformance for all reviewers
            var reviewerIds = reviewers.Select(r => r.Id).ToList();
            var reviewerPerfRepo = _unitOfWork.GetRepo<ReviewerPerformance>();
            var reviewerPerformances = (await reviewerPerfRepo.GetAllAsync(
                new QueryBuilder<ReviewerPerformance>()
                    .WithPredicate(rp => reviewerIds.Contains(rp.ReviewerId))
                    .Build()
            ))
            .GroupBy(rp => rp.ReviewerId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.LastUpdated).FirstOrDefault());

            // 4. Get ReviewerAssignment for each reviewer
            var reviewerAssignmentRepo = _unitOfWork.GetRepo<ReviewerAssignment>();
            var allAssignments = await reviewerAssignmentRepo.GetAllAsync(
                new QueryBuilder<ReviewerAssignment>()
                    .WithPredicate(a => reviewerIds.Contains(a.ReviewerId))
                    .Build()
            );
            var activeStatuses = new[] { AssignmentStatus.Assigned, AssignmentStatus.InProgress };
            var completedStatuses = new[] { AssignmentStatus.Completed };
            var reviewerAssignmentCounts = reviewers.ToDictionary(
                reviewer => reviewer.Id,
                reviewer =>
                {
                    var assignments = allAssignments.Where(a => a.ReviewerId == reviewer.Id);
                    int active = assignments.Count(a => activeStatuses.Contains(a.Status));
                    int completed = assignments.Count(a => completedStatuses.Contains(a.Status));
                    return (active, completed);
                });

            // 5. Get Gemini embedding for topic context
            var topicVec = await _aiService.GetEmbeddingAsync(topicContext);

            var suggestionList = new List<ReviewerSuggestionDTO>();
            foreach (var reviewer in reviewers)
            {
                var skillDict = reviewer.LecturerSkills.ToDictionary(s => s.SkillTag, s => s.ProficiencyLevel.ToString());
                var skillText = string.Join(", ", reviewer.LecturerSkills.Select(s => $"{s.SkillTag} ({s.ProficiencyLevel})"));
                var reviewerVec = await _aiService.GetEmbeddingAsync(skillText);

                decimal skillMatchScore = VectorMath.CosineSimilarity(topicVec, reviewerVec);

                var topicKeywords = topicContext.ToLower().Split(' ', ',', '.', ';', ':').Distinct();
                var matchedSkills = reviewer.LecturerSkills
                    .Where(s => topicKeywords.Contains(s.SkillTag.ToLower()))
                    .Select(s => s.SkillTag)
                    .ToList();

                reviewerAssignmentCounts.TryGetValue(reviewer.Id, out var assignInfo);
                int currActive = assignInfo.active;
                int completed = assignInfo.completed;
                decimal workloadScore = 1 - Math.Min(1, currActive / 5m);

                reviewerPerformances.TryGetValue(reviewer.Id, out var perf);
                decimal perfScore = perf != null
                    ? (perf.QualityRating ?? 0) * 0.5m + (perf.OnTimeRate ?? 0) * 0.3m + (perf.AverageScoreGiven ?? 0) * 0.2m
                    : 0;

                var isEligible = true;
                var ineligibilityReasons = new List<string>();
                if (currActive > 5) { isEligible = false; ineligibilityReasons.Add("Too many active assignments"); }
                if (perf?.QualityRating is decimal q && q < 2) { isEligible = false; ineligibilityReasons.Add("Low quality rating"); }
                decimal overallScore = skillMatchScore * 0.4m + workloadScore * 0.2m + perfScore * 0.3m;
                if (!isEligible) overallScore -= 0.5m;

                suggestionList.Add(new ReviewerSuggestionDTO
                {
                    ReviewerId = reviewer.Id,
                    ReviewerName = reviewer.Email,
                    SkillMatchScore = skillMatchScore,
                    MatchedSkills = matchedSkills,
                    ReviewerSkills = skillDict,
                    CurrentActiveAssignments = currActive,
                    CompletedAssignments = completed,
                    WorkloadScore = workloadScore,
                    AverageScoreGiven = perf?.AverageScoreGiven,
                    OnTimeRate = perf?.OnTimeRate,
                    QualityRating = perf?.QualityRating,
                    PerformanceScore = perfScore,
                    OverallScore = overallScore,
                    IsEligible = isEligible,
                    IneligibilityReasons = ineligibilityReasons
                });
            }

            // NEW: Prioritize by lowest currentActiveAssignments, then by overallScore DESC
            var sorted = suggestionList
                .OrderBy(x => x.CurrentActiveAssignments)
                .ThenByDescending(x => x.OverallScore)
                .Take(input.MaxSuggestions)
                .ToList();

            // Prompt-based explanation
            string? explanation = null;
            if (input.UsePrompt)
            {
                try
                {
                    string prompt = $@"Given the topic:
---
{topicContext}
---
and these reviewer expertise profiles:
{string.Join("\n", sorted.Select((s, i) => $"Reviewer {i + 1}: {string.Join(", ", s.ReviewerSkills.Select(kv => $"{kv.Key} ({kv.Value})"))}"))}
Suggest the most suitable reviewers (Reviewer 1, 2, 3) for this topic and explain why.";
                    explanation = await _aiService.GetPromptCompletionAsync(prompt);
                }
                catch (Exception ex)
                {
                    explanation = $"Prompt-based AI explanation failed: {ex.Message}";
                }
            }

            return new BaseResponseModel<ReviewerSuggestionOutputDTO>
            {
                Data = ReviewerSuggestionMapper.ToOutputDTO(sorted, explanation),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Gợi ý reviewer thành công"
            };
        }

        // Example GET: Return top reviewers by lowest current workload (fake data for demo)
        public async Task<BaseResponseModel<List<ReviewerSuggestionDTO>>> GetTopReviewersAsync(int count = 5)
        {
            // In real code, reuse the suggestion logic, but here is a mock for demo:
            var fake = Enumerable.Range(1, count).Select(i => new ReviewerSuggestionDTO
            {
                ReviewerId = i,
                ReviewerName = $"reviewer{i}@university.edu",
                SkillMatchScore = 0.8m + 0.04m * (count - i),
                MatchedSkills = new List<string> { "AI", "ML" },
                ReviewerSkills = new Dictionary<string, string> { { "AI", "Expert" }, { "ML", "Advanced" } },
                CurrentActiveAssignments = i - 1,
                CompletedAssignments = 10 + i,
                WorkloadScore = 1 - Math.Min(1, (i - 1) / 5m),
                AverageScoreGiven = 4 + 0.1m * i,
                OnTimeRate = 0.9m + 0.01m * i,
                QualityRating = 4.5m + 0.05m * i,
                PerformanceScore = 4.3m + 0.05m * i,
                OverallScore = 0.85m + 0.02m * (count - i),
                IsEligible = true,
                IneligibilityReasons = new List<string>()
            }).ToList();

            return new BaseResponseModel<List<ReviewerSuggestionDTO>>
            {
                Data = fake,
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Top reviewers retrieved successfully"
            };
        }
    }
}