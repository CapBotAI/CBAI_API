using System.ComponentModel.DataAnnotations;
using App.Commons;

namespace App.Entities.DTOs.Review;

public class CriteriaScoreDTO
{
    /// <summary>
    /// ID của tiêu chí đánh giá
    /// </summary>
    [Required(ErrorMessage = ConstantModel.Required)]
    public int CriteriaId { get; set; }

    /// <summary>
    /// Điểm số cho tiêu chí này
    /// </summary>
    [Required(ErrorMessage = "Điểm số là bắt buộc")]
    [Range(0, 100, ErrorMessage = "Điểm số phải từ 0 đến 100")]
    public decimal Score { get; set; }

    /// <summary>
    /// Nhận xét cho tiêu chí này
    /// </summary>
    [StringLength(500, ErrorMessage = "Nhận xét không được vượt quá 500 ký tự")]
    public string? Comment { get; set; }
}