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
    /// Minimum skill match score yêu cầu (0-5)
    /// </summary>
    [Range(0, 5, ErrorMessage = "Minimum skill match score phải từ 0 đến 5")]
    public decimal MinimumSkillMatchScore { get; set; } = 2.0m;

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
    /// Skill tags của đề tài (tùy chọn, để override auto detect)
    /// </summary>
    public List<string> TopicSkillTags { get; set; } = new();
}