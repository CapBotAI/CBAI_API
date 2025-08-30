using System;
using System.Collections.Generic;
using App.Commons;
using App.Entities.Entities.Core;

namespace App.Entities.Entities.App;

public partial class Topic : CommonDataModel
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Objectives { get; set; }

    public int SupervisorId { get; set; }

    public int? CategoryId { get; set; }

    public int SemesterId { get; set; }

    public int MaxStudents { get; set; } = 1;

    public bool IsLegacy { get; set; } = false;

    public bool IsApproved { get; set; } = false;

    public virtual User Supervisor { get; set; } = null!;
    public virtual TopicCategory? Category { get; set; }
    public virtual Semester Semester { get; set; } = null!;
    public virtual ICollection<TopicVersion> TopicVersions { get; set; } = new List<TopicVersion>();
}
