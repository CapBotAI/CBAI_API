using App.Entities.Enums;

namespace App.Entities.DTOs.Review;

public class SubmissionReviewSummaryDTO
{
    public int SubmissionId { get; set; }
    public string TopicTitle { get; set; } = null!;
    public string StudentName { get; set; } = null!;
    public SubmissionStatus SubmissionStatus { get; set; }
    public int RequiredReviewerCount { get; set; } = 2;
    public int CompletedReviewCount { get; set; }
    public decimal? FinalScore { get; set; }
    public DateTime? RevisionDeadline { get; set; }
    public bool IsConflicted { get; set; }
    public bool IsOverdue { get; set; }
    
    public List<ReviewSummaryDTO> Reviews { get; set; } = new List<ReviewSummaryDTO>();
}

public class ReviewSummaryDTO
{
    public int ReviewId { get; set; }
    public int ReviewerId { get; set; }
    public string ReviewerName { get; set; } = null!;
    public ReviewStatus Status { get; set; }
    public ReviewRecommendations Recommendation { get; set; }
    public decimal? OverallScore { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? RevisionDeadline { get; set; }
}