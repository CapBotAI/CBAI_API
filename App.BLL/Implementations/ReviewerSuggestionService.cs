using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.BLL.Interfaces;
using App.BLL.Mapping;
using App.BLL.Services;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.ReviewerSuggestion;
using App.Entities.Entities.App;

namespace App.BLL.Implementations
{
    /// <summary>
    /// Main logic for suggesting reviewers given a topic version, using Gemini API for skills, workload, and performance.
    /// </summary>
    public class ReviewerSuggestionService : IReviewerSuggestionService
    {
        private readonly GeminiAIService _aiService;
        private readonly IUnitOfWork _unitOfWork;

        public ReviewerSuggestionService(
            GeminiAIService aiService,
            IUnitOfWork unitOfWork)
        {
            _aiService = aiService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ReviewerSuggestionOutputDTO> SuggestReviewersAsync(ReviewerSuggestionInputDTO input)
        {
            // Load topic version
            var topicVersion = await _unitOfWork.TopicVersionRepository.GetByIdAsync(input.TopicVersionId)
                ?? throw new Exception("Topic version not found");

            // Load reviewers
            var reviewers = await _unitOfWork.UserRepository.GetAllReviewersAsync();

            // Load reviewer performances (current semester)
            var reviewerPerformances = (await _unitOfWork.ReviewerPerformanceRepository.GetCurrentSemesterPerformances())
                .ToDictionary(rp => rp.ReviewerId);

            // Build topic string and get embedding
            var topicText = $"{topicVersion.Title} {topicVersion.Description} {topicVersion.Objectives} {topicVersion.Methodology} {topicVersion.ExpectedOutcomes}";
            var topicVec = await _aiService.GetEmbeddingAsync(topicText);

            // Load workload info
            var activeAssignments = await _unitOfWork.ReviewerAssignmentRepository.GetActiveAssignmentsCountByReviewerAsync();
            var completedAssignments = await _unitOfWork.ReviewerAssignmentRepository.GetCompletedAssignmentsCountByReviewerAsync();

            var suggestionList = new List<ReviewerSuggestionDTO>();
            foreach (var reviewer in reviewers)
            {
                // Skill matching
                var skillDict = reviewer.LecturerSkills.ToDictionary(s => s.SkillTag, s => s.ProficiencyLevel.ToString());
                var skillText = string.Join(", ", reviewer.LecturerSkills.Select(s => $"{s.SkillTag} ({s.ProficiencyLevel})"));
                var reviewerVec = await _aiService.GetEmbeddingAsync(skillText);
                decimal skillMatchScore = VectorMath.CosineSimilarity(topicVec, reviewerVec);

                // Matched skills (intersection by keyword)
                var topicKeywords = topicText.ToLower().Split(' ', ',', '.', ';', ':').Distinct();
                var matchedSkills = reviewer.LecturerSkills
                    .Where(s => topicKeywords.Contains(s.SkillTag.ToLower()))
                    .Select(s => s.SkillTag)
                    .ToList();

                // Workload calculation
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

            // Optional: Gemini prompt-based explanation
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