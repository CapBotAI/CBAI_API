using App.Entities.DTOs.Accounts;
using App.Entities.DTOs.Topics;
using App.Entities.DTOs.TopicVersions;
using App.Entities.Enums;

namespace App.Entities.DTOs.ReviewerAssignment;

public class ReviewerAssignmentResponseDTO
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int ReviewerId { get; set; }
    public int AssignedBy { get; set; }
    public AssignmentTypes AssignmentType { get; set; }
    public decimal? SkillMatchScore { get; set; }
    public DateTime? Deadline { get; set; }
    public AssignmentStatus Status { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? TopicId { get; set; }

    // Navigation properties
    public UserOverviewDTO? Reviewer { get; set; }
    public UserOverviewDTO? AssignedByUser { get; set; }
    public string? SubmissionTitle { get; set; }
    public string? TopicTitle { get; set; }
    public TopicDetailDTO? Topic { get; set; }
    public TopicVersionDetailDTO ? TopicVersion { get; set; }

}