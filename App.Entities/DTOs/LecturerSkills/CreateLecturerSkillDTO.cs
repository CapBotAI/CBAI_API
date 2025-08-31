using System;
using System.ComponentModel.DataAnnotations;
using App.Commons;
using App.Entities.Entities.App;
using App.Entities.Enums;
using FS.Commons.Interfaces;

namespace App.Entities.DTOs.LecturerSkills;

public class CreateLecturerSkillDTO : IEntity<LecturerSkill>
{
    /// <summary>
    /// ID giảng viên. Admin có thể tạo cho bất kỳ giảng viên nào; giảng viên thường sẽ tự gán là chính mình.
    /// </summary>
    public int? LecturerId { get; set; }

    /// <summary>
    /// Tên/Tag kỹ năng
    /// </summary>
    [Required(ErrorMessage = ConstantModel.Required)]
    [StringLength(100, ErrorMessage = "Kỹ năng không được vượt quá 100 ký tự")]
    public string SkillTag { get; set; } = null!;

    /// <summary>
    /// Mức độ thành thạo
    /// </summary>
    public ProficiencyLevels ProficiencyLevel { get; set; } = ProficiencyLevels.Intermediate;

    public LecturerSkill GetEntity()
    {
        return new LecturerSkill
        {
            SkillTag = SkillTag,
            ProficiencyLevel = ProficiencyLevel,
            IsActive = true,
            DeletedAt = null
        };
    }

}