using System;
using System.Collections.Generic;

namespace App.Entities.Entities.App;

public partial class EvaluationCriteria
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int MaxScore { get; set; } = 10;

    public decimal Weight { get; set; } = 1.00m;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ReviewCriteriaScore> ReviewCriteriaScores { get; set; } = new List<ReviewCriteriaScore>();
}
