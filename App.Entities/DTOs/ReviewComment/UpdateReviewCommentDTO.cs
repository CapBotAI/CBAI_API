using System.ComponentModel.DataAnnotations;
using App.Entities.Enums;

namespace App.Entities.DTOs.ReviewComment;

public class UpdateReviewCommentDTO
{
    /// <summary>
    /// Tên phần/section được comment
    /// </summary>
    [StringLength(255, ErrorMessage = "Tên phần không được vượt quá 255 ký tự")]
    public string? SectionName { get; set; }

    /// <summary>
    /// Số dòng trong code/document được comment
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Nội dung comment
    /// </summary>
    [Required(ErrorMessage = "Nội dung comment là bắt buộc")]
    [StringLength(2000, ErrorMessage = "Nội dung comment không được vượt quá 2000 ký tự")]
    public string CommentText { get; set; } = null!;

    /// <summary>
    /// Loại comment
    /// </summary>
    public CommentTypes CommentType { get; set; }

    /// <summary>
    /// Mức độ ưu tiên
    /// </summary>
    public PriorityLevels Priority { get; set; }

    /// <summary>
    /// Trạng thái đã giải quyết hay chưa
    /// </summary>
    public bool IsResolved { get; set; }
}