using System;
using System.Collections.Generic;
using App.Commons;

namespace App.Entities.Entities.App;

public partial class Phase : CommonDataModel
{
    public int Id { get; set; }

    public int SemesterId { get; set; }

    public int PhaseTypeId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime? SubmissionDeadline { get; set; }

    public virtual Semester Semester { get; set; } = null!;
    public virtual PhaseType PhaseType { get; set; } = null!;
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
