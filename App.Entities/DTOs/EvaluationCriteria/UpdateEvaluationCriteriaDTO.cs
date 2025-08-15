using System.ComponentModel.DataAnnotations;
using App.Commons;

namespace App.Entities.DTOs.EvaluationCriteria;

public class UpdateEvaluationCriteriaDTO
{
    /// <summary>
    /// ID của tiêu chí cần cập nhật
    /// </summary>
    [Required(ErrorMessage = ConstantModel.Required)]
    public int Id { get; set; }

    /// <summary>
    /// Tên tiêu chí đánh giá
    /// </summary>
    [Required(ErrorMessage = ConstantModel.Required)]
    [StringLength(255, ErrorMessage = "Tên tiêu chí không được vượt quá 255 ký tự")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Mô tả chi tiết tiêu chí
    /// </summary>
    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
    public string? Description { get; set; }

    /// <summary>
    /// Điểm tối đa cho tiêu chí này
    /// </summary>
    [Range(1, 100, ErrorMessage = "Điểm tối đa phải từ 1 đến 100")]
    public int MaxScore { get; set; } = 10;

    /// <summary>
    /// Trọng số của tiêu chí (0.1 - 10.0)
    /// </summary>
    [Range(0.1, 10.0, ErrorMessage = "Trọng số phải từ 0.1 đến 10.0")]
    public decimal Weight { get; set; } = 1.00m;
}