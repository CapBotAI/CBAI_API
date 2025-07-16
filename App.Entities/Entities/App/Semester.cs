using System;
using System.Collections.Generic;

namespace App.Entities.Entities.App;

public partial class Semester
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsActive { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Phase> Phases { get; set; } = new List<Phase>();
    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public virtual ICollection<ReviewerPerformance> ReviewerPerformances { get; set; } = new List<ReviewerPerformance>();
}
