// App.BLL/Interfaces/INotificationService.cs
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Notifications;

namespace App.BLL.Interfaces;

public interface INotificationService
{
    Task<BaseResponseModel<NotificationResponseDTO>> CreateAsync(CreateNotificationDTO dto);
    Task<BaseResponseModel<List<int>>> CreateBulkAsync(CreateBulkNotificationsDTO dto);

    Task<BaseResponseModel<PagingDataModel<NotificationResponseDTO, GetNotificationsQueryDTO>>> GetMyAsync(int userId, GetNotificationsQueryDTO query);
    Task<BaseResponseModel<object>> CountUnreadAsync(int userId);

    Task<BaseResponseModel> MarkAsReadAsync(int userId, int notificationId);
    Task<BaseResponseModel> MarkAllAsReadAsync(int userId);
}