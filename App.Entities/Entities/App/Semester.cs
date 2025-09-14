using System;
using System.Collections.Generic;
using App.Commons;

namespace App.Entities.Entities.App;

public partial class Semester : CommonDataModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Phase> Phases { get; set; } = new List<Phase>();
    public virtual ICollection<EvaluationCriteria> EvaluationCriterias { get; set; } = new List<EvaluationCriteria>();

    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public virtual ICollection<ReviewerPerformance> ReviewerPerformances { get; set; } = new List<ReviewerPerformance>();
}