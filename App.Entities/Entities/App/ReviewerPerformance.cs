using System;
using App.Entities.Entities.Core;

namespace App.Entities.Entities.App;

public partial class ReviewerPerformance
{
    public int Id { get; set; }

    public int ReviewerId { get; set; }

    public int SemesterId { get; set; }

    public int TotalAssignments { get; set; } = 0;

    public int CompletedAssignments { get; set; } = 0;

    public int AverageTimeMinutes { get; set; } = 0;

    public decimal? AverageScoreGiven { get; set; }

    public decimal? OnTimeRate { get; set; }

    public decimal? QualityRating { get; set; }

    public DateTime LastUpdated { get; set; }

    public virtual User Reviewer { get; set; } = null!;
    public virtual Semester Semester { get; set; } = null!;
}
