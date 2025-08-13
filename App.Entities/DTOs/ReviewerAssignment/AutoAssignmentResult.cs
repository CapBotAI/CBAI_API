namespace App.Entities.DTOs.ReviewerAssignment;

public class AutoAssignmentResult
{
    public int SubmissionId { get; set; }
    public string? TopicTitle { get; set; }
    public List<string> TopicSkillTags { get; set; } = new();
    
    public List<ReviewerAssignmentResponseDTO> AssignedReviewers { get; set; } = new();
    public List<ReviewerMatchingResult> ConsideredReviewers { get; set; } = new();
    
    public int RequestedReviewers { get; set; }
    public int AssignedCount { get; set; }
    public bool IsFullyAssigned { get; set; }
    
    public List<string> Warnings { get; set; } = new();
}