using App.Commons.Paging;
using App.Entities.Enums;

namespace App.Entities.DTOs.Notifications;

public class GetNotificationsQueryDTO : PagingModel
{
    public bool? IsRead { get; set; }
    public NotificationTypes? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}