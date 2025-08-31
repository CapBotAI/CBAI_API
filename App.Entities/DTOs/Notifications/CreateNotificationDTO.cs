using App.Entities.Entities.App;
using App.Entities.Enums;
using FS.Commons.Interfaces;

namespace App.Entities.DTOs.Notifications;

public class CreateNotificationDTO : IEntity<SystemNotification>
{
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public NotificationTypes Type { get; set; } = NotificationTypes.Info;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }

    public SystemNotification GetEntity()
    {
        return new SystemNotification
        {
            UserId = UserId,
            Title = Title,
            Message = Message,
            Type = Type,
            RelatedEntityType = RelatedEntityType,
            RelatedEntityId = RelatedEntityId,
            IsRead = false,
            CreatedAt = DateTime.Now
        };
    }

}