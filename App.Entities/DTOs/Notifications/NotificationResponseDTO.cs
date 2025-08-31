using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.Notifications;

public class NotificationResponseDTO
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public NotificationTypes Type { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public NotificationResponseDTO() { }
    public NotificationResponseDTO(SystemNotification e)
    {
        Id = e.Id;
        UserId = e.UserId;
        Title = e.Title;
        Message = e.Message;
        Type = e.Type;
        RelatedEntityType = e.RelatedEntityType;
        RelatedEntityId = e.RelatedEntityId;
        IsRead = e.IsRead;
        CreatedAt = e.CreatedAt;
        ReadAt = e.ReadAt;
    }
}