using System.ComponentModel.DataAnnotations;
using App.Commons;
using App.Entities.Enums;

namespace App.Entities.DTOs.ReviewerAssignment;

public class AutoAssignReviewerDTO
{
    /// <summary>
    /// ID của submission cần auto assign reviewer
    /// </summary>
    [Required(ErrorMessage = ConstantModel.Required)]
    public int SubmissionId { get; set; }

    /// <summary>
    /// Số lượng reviewer cần assign (mặc định 1)
    /// </summary>
    [Range(1, 5, ErrorMessage = "Số lượng reviewer phải từ 1 đến 5")]
    public int NumberOfReviewers { get; set; } = 1;

    /// <summary>
    /// Loại assignment (Primary, Secondary, Additional)
    /// </summary>
    public AssignmentTypes AssignmentType { get; set; } = AssignmentTypes.Primary;

    /// <summary>
    /// Hạn deadline cho việc review (tùy chọn)
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Minimum performance score yêu cầu (0-5) - thay thế cho skill match score
    /// </summary>
    [Range(0, 5, ErrorMessage = "Minimum performance score phải từ 0 đến 5")]
    public decimal MinimumSkillMatchScore { get; set; } = 2.5m; // Rename property later to MinimumPerformanceScore

    /// <summary>
    /// Maximum workload cho reviewer (số assignment đang active)
    /// </summary>
    [Range(1, 20, ErrorMessage = "Maximum workload phải từ 1 đến 20")]
    public int MaxWorkload { get; set; } = 10;

    /// <summary>
    /// Có ưu tiên reviewer có performance cao không
    /// </summary>
    public bool PrioritizeHighPerformance { get; set; } = true;

    /// <summary>
    /// Minimum quality rating yêu cầu (chỉ áp dụng khi PrioritizeHighPerformance = true)
    /// </summary>
    [Range(0, 5, ErrorMessage = "Minimum quality rating phải từ 0 đến 5")]
    public decimal MinimumQualityRating { get; set; } = 3.0m;

    /// <summary>
    /// Minimum on-time rate yêu cầu (0-1, chỉ áp dụng khi PrioritizeHighPerformance = true)
    /// </summary>
    [Range(0, 1, ErrorMessage = "Minimum on-time rate phải từ 0 đến 1")]
    public decimal MinimumOnTimeRate { get; set; } = 0.7m;

    /// <summary>
    /// Có ưu tiên reviewer có kinh nghiệm không (nhiều assignment đã hoàn thành)
    /// </summary>
    public bool PrioritizeExperience { get; set; } = false;
}