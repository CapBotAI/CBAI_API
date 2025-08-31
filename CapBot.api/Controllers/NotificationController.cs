// CapBot.api/Controllers/NotificationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using App.BLL.Interfaces;
using App.Commons.BaseAPI;
using App.Entities.DTOs.Notifications;

namespace CapBot.api.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : BaseAPIController
    {
        private readonly INotificationService _service;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService service, ILogger<NotificationController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Danh sách thông báo của tôi")]
        public async Task<IActionResult> GetMy([FromQuery] GetNotificationsQueryDTO query)
        {
            try
            {
                var result = await _service.GetMyAsync(UserId, query);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get notifications error for user {UserId}", UserId);
                return Error("Đã xảy ra lỗi!");
            }
        }

        [HttpGet("unread-count")]
        [SwaggerOperation(Summary = "Đếm số thông báo chưa đọc của tôi")]
        public async Task<IActionResult> CountUnread()
        {
            try
            {
                var result = await _service.CountUnreadAsync(UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Count unread notifications error for user {UserId}", UserId);
                return Error("Đã xảy ra lỗi!");
            }
        }

        [HttpPut("{id}/read")]
        [SwaggerOperation(Summary = "Đánh dấu đã đọc 1 thông báo")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var result = await _service.MarkAsReadAsync(UserId, id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkAsRead error for notification {Id} by user {UserId}", id, UserId);
                return Error("Đã xảy ra lỗi!");
            }
        }

        [HttpPut("read-all")]
        [SwaggerOperation(Summary = "Đánh dấu đã đọc tất cả")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var result = await _service.MarkAllAsReadAsync(UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkAllAsRead error by user {UserId}", UserId);
                return Error("Đã xảy ra lỗi!");
            }
        }

        // Tùy chọn: endpoint chỉ admin - tạo thông báo hệ thống
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Tạo thông báo cho 1 người dùng (admin)")]
        public async Task<IActionResult> Create([FromBody] CreateNotificationDTO dto)
        {
            if (!ModelState.IsValid) return ModelInvalid();
            try
            {
                var result = await _service.CreateAsync(dto);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create notification error by Admin");
                return Error("Đã xảy ra lỗi!");
            }
        }

        [HttpPost("bulk")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Tạo thông báo hàng loạt (admin)")]
        public async Task<IActionResult> CreateBulk([FromBody] CreateBulkNotificationsDTO dto)
        {
            if (!ModelState.IsValid) return ModelInvalid();
            try
            {
                var result = await _service.CreateBulkAsync(dto);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create bulk notifications error by Admin");
                return Error("Đã xảy ra lỗi!");
            }
        }
    }
}