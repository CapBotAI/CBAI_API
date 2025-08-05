using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

namespace App.BLL.Implementations
{
    /// <summary>
    /// Reviewer suggestion logic using Gemini API.
    /// Fetches and analyzes Users (with Reviewer role), LecturerSkills, ReviewerPerformance,
    /// ReviewerAssignment, Topic, TopicCategory, TopicVersion to sharpen reviewer selection.
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

        public async Task<BaseResponseModel> SuggestReviewersAsync(ReviewerSuggestionInputDTO input)
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
                throw new Exception("Topic version not found.");

            var topic = topicVersion.Topic;
            var topicCategory = topic?.Category;

            // 2. Build context string for Gemini embedding
            var topicContext = $"{topicCategory?.Name ?? ""} {topic?.Title ?? ""} {topicVersion.Title ?? ""} {topicVersion.Description ?? ""} {topicVersion.Objectives ?? ""} {topicVersion.Methodology ?? ""} {topicVersion.ExpectedOutcomes ?? ""}";

            // 3. Get reviewers: Users with Reviewer role and at least one LecturerSkill
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

            // 4. Get ReviewerPerformance for all reviewers
            var reviewerIds = reviewers.Select(r => r.Id).ToList();
            var reviewerPerfRepo = _unitOfWork.GetRepo<ReviewerPerformance>();
            var reviewerPerformances = (await reviewerPerfRepo.GetAllAsync(
                new QueryBuilder<ReviewerPerformance>()
                    .WithPredicate(rp => reviewerIds.Contains(rp.ReviewerId))
                    .Build()
            ))
            .GroupBy(rp => rp.ReviewerId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.LastUpdated).First());

            // 5. Workload: Get ReviewerAssignment for each reviewer
            var reviewerAssignmentRepo = _unitOfWork.GetRepo<ReviewerAssignment>();
            var allAssignments = await reviewerAssignmentRepo.GetAllAsync(
                new QueryBuilder<ReviewerAssignment>()
                    .WithPredicate(a => reviewerIds.Contains(a.ReviewerId))
                    .Build()
            );
            // Define what statuses are considered "active" and "completed" in your project
            var activeStatuses = new[] { AssignmentStatus.Assigned, AssignmentStatus.InProgress }; // adjust to your codebase
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

            // 6. Get Gemini embedding for topic context
            var topicVec = await _aiService.GetEmbeddingAsync(topicContext);

            var suggestionList = new List<ReviewerSuggestionDTO>();

            foreach (var reviewer in reviewers)
            {
                // Skill string for reviewer (LecturerSkill)
                var skillDict = reviewer.LecturerSkills.ToDictionary(s => s.SkillTag, s => s.ProficiencyLevel.ToString());
                var skillText = string.Join(", ", reviewer.LecturerSkills.Select(s => $"{s.SkillTag} ({s.ProficiencyLevel})"));
                var reviewerVec = await _aiService.GetEmbeddingAsync(skillText);

                // Skill match: cosine similarity
                decimal skillMatchScore = VectorMath.CosineSimilarity(topicVec, reviewerVec);

                // Matched skills: intersection with topic keywords
                var topicKeywords = topicContext.ToLower().Split(' ', ',', '.', ';', ':').Distinct();
                var matchedSkills = reviewer.LecturerSkills
                    .Where(s => topicKeywords.Contains(s.SkillTag.ToLower()))
                    .Select(s => s.SkillTag)
                    .ToList();

                // Workload
                reviewerAssignmentCounts.TryGetValue(reviewer.Id, out var assignInfo);
                int currActive = assignInfo.active;
                int completed = assignInfo.completed;
                decimal workloadScore = 1 - Math.Min(1, currActive / 5m);

                // ReviewerPerformance
                reviewerPerformances.TryGetValue(reviewer.Id, out var perf);
                decimal perfScore = perf != null
                    ? (perf.QualityRating ?? 0) * 0.5m + (perf.OnTimeRate ?? 0) * 0.3m + (perf.AverageScoreGiven ?? 0) * 0.2m
                    : 0;

                // Eligibility
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

            var sorted = suggestionList.OrderByDescending(x => x.OverallScore).Take(input.MaxSuggestions).ToList();

            // Optional: Gemini prompt-based explanation
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
    }
}