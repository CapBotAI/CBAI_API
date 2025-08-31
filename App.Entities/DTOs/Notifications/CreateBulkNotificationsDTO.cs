using App.Entities.Enums;

namespace App.Entities.DTOs.Notifications;

public class CreateBulkNotificationsDTO
{
    public List<int> UserIds { get; set; } = new();
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public NotificationTypes Type { get; set; } = NotificationTypes.Info;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}