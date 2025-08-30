using App.Entities.Enums;

namespace App.Entities.DTOs.ReviewComment;

public class ReviewCommentResponseDTO
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public string? SectionName { get; set; }
    public int? LineNumber { get; set; }
    public string CommentText { get; set; } = null!;
    public CommentTypes CommentType { get; set; }
    public string CommentTypeName { get; set; } = null!;
    public PriorityLevels Priority { get; set; }
    public string PriorityName { get; set; } = null!;
    public bool IsResolved { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    
    // Navigation properties
    public string? ReviewerName { get; set; }
    public string? ReviewTitle { get; set; }
}