// App.Entities/DTOs/LecturerSkills/UpdateLecturerSkillDTO.cs
using System.ComponentModel.DataAnnotations;
using App.Commons;
using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.LecturerSkills;

public class UpdateLecturerSkillDTO
{
    [Required(ErrorMessage = ConstantModel.Required)]
    public int Id { get; set; }

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