using System;
using System.Collections.Generic;
using App.Commons;
using App.Entities.Enums;

namespace App.Entities.Entities.App;

public partial class Review : CommonDataModel
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }

    public decimal? OverallScore { get; set; }

    public string? OverallComment { get; set; }

    public ReviewRecommendations Recommendation { get; set; } = ReviewRecommendations.MinorRevision;

    public int? TimeSpentMinutes { get; set; }

    public ReviewStatus Status { get; set; } = ReviewStatus.Draft;

    public DateTime? SubmittedAt { get; set; }

    public virtual ReviewerAssignment Assignment { get; set; } = null!;
    public virtual ICollection<ReviewCriteriaScore> ReviewCriteriaScores { get; set; } = new List<ReviewCriteriaScore>();
    public virtual ICollection<ReviewComment> ReviewComments { get; set; } = new List<ReviewComment>();
}
