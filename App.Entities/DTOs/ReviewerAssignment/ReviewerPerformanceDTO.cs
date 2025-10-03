using System;

namespace App.Entities.DTOs.ReviewerAssignment;

public class ReviewerPerformanceDTO
{
    public int Id { get; set; }
    public int ReviewerId { get; set; }
    public int SemesterId { get; set; }
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int AverageTimeMinutes { get; set; }
    public decimal? AverageScoreGiven { get; set; }
    public decimal? OnTimeRate { get; set; }
    public decimal? QualityRating { get; set; }
    public DateTime LastUpdated { get; set; }
}
