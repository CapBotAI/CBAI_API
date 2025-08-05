using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.BLL.Interfaces;
using App.BLL.Mapping;
using App.BLL.Services;
using App.Entities.DTOs.ReviewerSuggestion;
using App.Entities.Entities.App;
using App.Entities.Entities.Core;

namespace App.BLL.Implementations
{
    /// <summary>
    /// Main logic for suggesting reviewers given a topic version, using Gemini API for skills, workload, and performance.
    /// </summary>
    public class ReviewerSuggestionService : IReviewerSuggestionService
    {
        private readonly GeminiAIService _aiService;
        private readonly IUserRepository _userRepository;
        private readonly IReviewerPerformanceRepository _reviewerPerformanceRepository;
        private readonly ITopicVersionRepository _topicVersionRepository;
        private readonly IReviewerAssignmentRepository _reviewerAssignmentRepository;

        public ReviewerSuggestionService(
            GeminiAIService aiService,
            IUserRepository userRepository,
            IReviewerPerformanceRepository reviewerPerformanceRepository,
            ITopicVersionRepository topicVersionRepository,
            IReviewerAssignmentRepository reviewerAssignmentRepository)
        {
            _aiService = aiService;
            _userRepository = userRepository;
            _reviewerPerformanceRepository = reviewerPerformanceRepository;
            _topicVersionRepository = topicVersionRepository;
            _reviewerAssignmentRepository = reviewerAssignmentRepository;
        }

        public async Task<ReviewerSuggestionOutputDTO> SuggestReviewersAsync(ReviewerSuggestionInputDTO input)
        {
            // Load topic, reviewers, performance, workload
            var topicVersion = await _topicVersionRepository.GetByIdAsync(input.TopicVersionId)
                ?? throw new Exception("Topic version not found");
            var reviewers = await _userRepository.GetAllReviewersAsync();
            var reviewerPerformances = (await _reviewerPerformanceRepository.GetCurrentSemesterPerformances())
                .ToDictionary(rp => rp.ReviewerId);
            var topicText = $"{topicVersion.Title} {topicVersion.Description} {topicVersion.Objectives} {topicVersion.Methodology} {topicVersion.ExpectedOutcomes}";
            var topicVec = await _aiService.GetEmbeddingAsync(topicText);
            var activeAssignments = await _reviewerAssignmentRepository.GetActiveAssignmentsCountByReviewerAsync();
            var completedAssignments = await _reviewerAssignmentRepository.GetCompletedAssignmentsCountByReviewerAsync();

            var suggestionList = new List<ReviewerSuggestionDTO>();
            foreach (var reviewer in reviewers)
            {
                // Skill matching
                var skillDict = reviewer.LecturerSkills.ToDictionary(s => s.SkillTag, s => s.ProficiencyLevel.ToString());
                var skillText = string.Join(", ", reviewer.LecturerSkills.Select(s => $"{s.SkillTag} ({s.ProficiencyLevel})"));
                var reviewerVec = await _aiService.GetEmbeddingAsync(skillText);
                decimal skillMatchScore = VectorMath.CosineSimilarity(topicVec, reviewerVec);

                // Matched skills: simple intersection
                var topicKeywords = topicText.ToLower().Split(' ', ',', '.', ';', ':').Distinct();
                var matchedSkills = reviewer.LecturerSkills
                    .Where(s => topicKeywords.Contains(s.SkillTag.ToLower()))
                    .Select(s => s.SkillTag)
                    .ToList();

                // Workload
                int currActive = activeAssignments.TryGetValue(reviewer.Id, out var ca) ? ca : 0;
                int completed = completedAssignments.TryGetValue(reviewer.Id, out var co) ? co : 0;
                decimal workloadScore = 1 - Math.Min(1, currActive / 5m);

                // Performance
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
                    ReviewerName = reviewer.FullName,
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

            // Optional: prompt-based Gemini explanation
            string? explanation = null;
            if (input.UsePrompt)
            {
                try
                {
                    string prompt = $@"Given the topic:
---
{topicText}
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

            return ReviewerSuggestionMapper.ToOutputDTO(sorted, explanation);
        }
    }
}