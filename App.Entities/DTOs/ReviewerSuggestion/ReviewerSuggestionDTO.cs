using System.Collections.Generic;

namespace App.Entities.DTOs.ReviewerSuggestion
{
    /// <summary>
    /// Reviewer suggestion detail for each candidate.
    /// </summary>
    public class ReviewerSuggestionDTO
    {
        public int ReviewerId { get; set; }
        public string ReviewerName { get; set; }
        // Skill matching
        public decimal SkillMatchScore { get; set; }
        public List<string> MatchedSkills { get; set; } = new();
        public Dictionary<string, string> ReviewerSkills { get; set; } = new();
        // Workload
        public int CurrentActiveAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public decimal WorkloadScore { get; set; }
        // Performance
        public decimal? AverageScoreGiven { get; set; }
        public decimal? OnTimeRate { get; set; }
        public decimal? QualityRating { get; set; }
        public decimal PerformanceScore { get; set; }
        // Overall
        public decimal OverallScore { get; set; }
        public bool IsEligible { get; set; }
        public List<string> IneligibilityReasons { get; set; } = new();
    }
}