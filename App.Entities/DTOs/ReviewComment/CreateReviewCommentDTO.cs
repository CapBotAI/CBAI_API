using System.ComponentModel.DataAnnotations;
using App.Commons;
using App.Entities.Enums;

namespace App.Entities.DTOs.ReviewComment;

public class CreateReviewCommentDTO
{
    /// <summary>
    /// ID của Review mà comment này thuộc về
    /// </summary>
    [Required(ErrorMessage = ConstantModel.Required)]
    public int ReviewId { get; set; }

    /// <summary>
    /// Tên phần/section được comment (VD: "Authentication", "Database Design")
    /// </summary>
    [StringLength(255, ErrorMessage = "Tên phần không được vượt quá 255 ký tự")]
    public string? SectionName { get; set; }

    /// <summary>
    /// Số dòng trong code/document được comment (nếu có)
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Nội dung comment
    /// </summary>
    [Required(ErrorMessage = ConstantModel.Required)]
    [StringLength(2000, ErrorMessage = "Nội dung comment không được vượt quá 2000 ký tự")]
    public string CommentText { get; set; } = null!;

    /// <summary>
    /// Loại comment: Suggestion(1), Question(2), Correction(3), Praise(4)
    /// </summary>
    public CommentTypes CommentType { get; set; } = CommentTypes.Suggestion;

    /// <summary>
    /// Mức độ ưu tiên: Low(1), Medium(2), High(3), Critical(4)
    /// </summary>
    public PriorityLevels Priority { get; set; } = PriorityLevels.Medium;
}