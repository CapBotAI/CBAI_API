using App.Entities.Enums;

namespace App.Entities.DTOs.ReviewerAssignment;

public class AssignmentDetailsDTO
{
    public int AssignmentId { get; set; }
    public AssignmentStatus Status { get; set; }
    public AssignmentTypes AssignmentType { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? SkillMatchScore { get; set; }

    // Reviewer Info
    public int ReviewerId { get; set; }
    public string ReviewerName { get; set; } = null!;
    public string ReviewerEmail { get; set; } = null!;

    // Submission Info
    public int SubmissionId { get; set; }
    public SubmissionStatus SubmissionStatus { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? DocumentUrl { get; set; }
    public string? AdditionalNotes { get; set; }

    // Topic Info
    public int TopicId { get; set; }
    public string TopicTitle { get; set; } = null!;
    public string? TopicDescription { get; set; }
    public string? TopicObjectives { get; set; }

    // Student Info
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentEmail { get; set; } = null!;

    // Phase Info
    public int PhaseId { get; set; }
    public string PhaseName { get; set; } = null!;

    // Review Info (nếu có)
    public List<ReviewSummaryDTO> Reviews { get; set; } = new List<ReviewSummaryDTO>();

    public bool IsOverdue { get; set; }
    public bool CanStartReview { get; set; }
    public bool HasActiveReview { get; set; }
}

public class ReviewSummaryDTO
{
    public int ReviewId { get; set; }
    public ReviewStatus Status { get; set; }
    public ReviewRecommendations Recommendation { get; set; }
    public decimal? OverallScore { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? TimeSpentMinutes { get; set; }
}
