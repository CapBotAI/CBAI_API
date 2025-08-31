// App.Entities/DTOs/LecturerSkills/LecturerSkillResponseDTO.cs
using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.LecturerSkills;

public class LecturerSkillResponseDTO
{
    public int Id { get; set; }
    public int LecturerId { get; set; }
    public string SkillTag { get; set; } = null!;
    public ProficiencyLevels ProficiencyLevel { get; set; }
    public string ProficiencyLevelName { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    public LecturerSkillResponseDTO(LecturerSkill lecturerSkill)
    {
        Id = lecturerSkill.Id;
        LecturerId = lecturerSkill.LecturerId;
        SkillTag = lecturerSkill.SkillTag;
        ProficiencyLevel = lecturerSkill.ProficiencyLevel;
        ProficiencyLevelName = lecturerSkill.ProficiencyLevel.ToString();
        CreatedAt = lecturerSkill.CreatedAt;
        CreatedBy = lecturerSkill.CreatedBy;
        LastModifiedAt = lecturerSkill.LastModifiedAt;
        LastModifiedBy = lecturerSkill.LastModifiedBy;
    }
}