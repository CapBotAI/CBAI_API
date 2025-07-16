using System;
using System.Collections.Generic;
using App.Commons;
using App.Entities.Entities.Core;
using App.Entities.Enums;

namespace App.Entities.Entities.App;

public partial class LecturerSkill : CommonDataModel
{
    public int Id { get; set; }

    public int LecturerId { get; set; }

    public string SkillTag { get; set; } = null!;

    public ProficiencyLevels ProficiencyLevel { get; set; } = ProficiencyLevels.Intermediate;

    public virtual User Lecturer { get; set; } = null!;
}
