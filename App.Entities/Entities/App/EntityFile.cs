using System;
using App.Entities.Enums;

namespace App.Entities.Entities.App;

public class EntityFile
{
    public long Id { get; set; }
    public long FileId { get; set; }
    public EntityType EntityType { get; set; }
    public long EntityId { get; set; }
    public bool IsPrimary { get; set; }
    public string? Caption { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual AppFile? File { get; set; }
}
