using System;
using System.Collections.Generic;
using App.Commons;

namespace App.Entities.Entities.App;

public partial class EvaluationCriteria : CommonDataModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int MaxScore { get; set; } = 10;

    public decimal Weight { get; set; } = 1.00m;

    public int? SemesterId { get; set; }

    public virtual Semester? Semester { get; set; }

    public virtual ICollection<ReviewCriteriaScore> ReviewCriteriaScores { get; set; } = new List<ReviewCriteriaScore>();
}