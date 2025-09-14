using System;
using System.Collections.Generic;
using App.Commons;
using App.Entities.Entities.Core;

namespace App.Entities.Entities.App;

public partial class Topic : CommonDataModel
{
    public int Id { get; set; }

    public string EN_Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Objectives { get; set; }

    public int SupervisorId { get; set; }

    public int? CategoryId { get; set; }

    public int SemesterId { get; set; }

    public int MaxStudents { get; set; } = 1;

    public bool IsLegacy { get; set; } = false;

    public bool IsApproved { get; set; } = false;

    public string? Abbreviation { get; set; }
    public string? VN_title { get; set; }
    public string? Problem { get; set; }

    public string? Context { get; set; }
    public string? Content { get; set; }

    public virtual User Supervisor { get; set; } = null!;
    public virtual TopicCategory? Category { get; set; }
    public virtual Semester Semester { get; set; } = null!;
    public virtual ICollection<TopicVersion> TopicVersions { get; set; } = new List<TopicVersion>();
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}