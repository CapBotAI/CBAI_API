using System;
using System.Collections.Generic;

namespace App.Entities.Entities.App;

public partial class TopicCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
