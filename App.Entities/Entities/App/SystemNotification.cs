using System;
using App.Entities.Entities.Core;
using App.Entities.Enums;

namespace App.Entities.Entities.App;

public partial class SystemNotification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public NotificationTypes Type { get; set; } = NotificationTypes.Info;

    public string? RelatedEntityType { get; set; }

    public int? RelatedEntityId { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public virtual User User { get; set; } = null!;
}
