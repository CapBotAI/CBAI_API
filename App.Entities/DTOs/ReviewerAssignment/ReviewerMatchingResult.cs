using App.Entities.Enums;

namespace App.Entities.DTOs.ReviewerAssignment;

public class ReviewerMatchingResult
{
    public int ReviewerId { get; set; }
    public string? ReviewerName { get; set; }
    public string? ReviewerEmail { get; set; }
    
    // Skill matching
    public decimal SkillMatchScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public Dictionary<string, ProficiencyLevels> ReviewerSkills { get; set; } = new();
    
    // Workload info
    public int CurrentActiveAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public decimal WorkloadScore { get; set; }
    
    // Performance info
    public decimal? AverageScoreGiven { get; set; }
    public decimal? OnTimeRate { get; set; }
    public decimal? QualityRating { get; set; }
    public decimal PerformanceScore { get; set; }
    
    // Overall matching
    public decimal OverallScore { get; set; }
    public bool IsEligible { get; set; }
    public List<string> IneligibilityReasons { get; set; } = new();
}