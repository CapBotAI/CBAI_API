using App.Entities.Enums;

namespace App.Entities.DTOs.ReviewerAssignment;

public class AvailableReviewerDTO
{
    public int Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    // Workload information
    public int CurrentAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public decimal? AverageScoreGiven { get; set; }
    public decimal? OnTimeRate { get; set; }
    public decimal? QualityRating { get; set; }

    // Skill matching
    public List<string> Skills { get; set; } = new();
    public decimal? SkillMatchScore { get; set; }

    // Availability
    public bool IsAvailable { get; set; } = true;
    public string? UnavailableReason { get; set; }
}