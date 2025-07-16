using System;
using System.Collections.Generic;
using App.Commons;

namespace App.Entities.Entities.App;

public partial class PhaseType : CommonDataModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Phase> Phases { get; set; } = new List<Phase>();
}
