using App.BLL.Interfaces;
using App.Commons.Interfaces;
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.DAL.Queries;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Notifications;
using App.Entities.Entities.App;
using App.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Implementations;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationBroadcaster _broadcaster;

    public NotificationService(IUnitOfWork uow, INotificationBroadcaster broadcaster)
    {
        _uow = uow;
        _broadcaster = broadcaster;
    }

    public async Task<BaseResponseModel<NotificationResponseDTO>> CreateAsync(CreateNotificationDTO dto)
    {
        try
        {
            var repo = _uow.GetRepo<SystemNotification>();
            var entity = new SystemNotification
            {
                UserId = dto.UserId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                RelatedEntityType = dto.RelatedEntityType,
                RelatedEntityId = dto.RelatedEntityId,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            await repo.CreateAsync(entity);
            var save = await _uow.SaveAsync();
            if (!save.IsSuccess)
            {
                return new BaseResponseModel<NotificationResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = save.Message
                };
            }

            var dtoRes = new NotificationResponseDTO(entity);

            await _broadcaster.SendToUserAsync(entity.UserId, "notification", dtoRes);

            var unreadCount = await repo.Get(new QueryOptions<SystemNotification>
            {
                Predicate = x => x.UserId == entity.UserId && !x.IsRead
            }).CountAsync();

            await _broadcaster.SendToUserAsync(entity.UserId, NotificationMethod.NotificationUnreadCount.ToString(), unreadCount);

            return new BaseResponseModel<NotificationResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo thông báo thành công",
                Data = dtoRes
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<NotificationResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<List<int>>> CreateBulkAsync(CreateBulkNotificationsDTO dto)
    {
        try
        {
            var repo = _uow.GetRepo<SystemNotification>();
            var now = DateTime.Now;
            foreach (var uid in dto.UserIds.Distinct())
            {
                await repo.CreateAsync(new SystemNotification
                {
                    UserId = uid,
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = dto.Type,
                    RelatedEntityType = dto.RelatedEntityType,
                    RelatedEntityId = dto.RelatedEntityId,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            var save = await _uow.SaveAsync();
            if (!save.IsSuccess)
            {
                return new BaseResponseModel<List<int>>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = save.Message
                };
            }

            foreach (var uid in dto.UserIds.Distinct())
            {
                var unreadCount = await repo.Get(new QueryOptions<SystemNotification>
                {
                    Predicate = x => x.UserId == uid && !x.IsRead
                }).CountAsync();

                await _broadcaster.SendToUserAsync(uid, NotificationMethod.NotificationBulkCreated.ToString(), new { });
                await _broadcaster.SendToUserAsync(uid, NotificationMethod.NotificationUnreadCount.ToString(), unreadCount);
            }

            return new BaseResponseModel<List<int>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo thông báo hàng loạt thành công",
                Data = new List<int> { dto.UserIds.Distinct().Count() }
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<List<int>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<NotificationResponseDTO, GetNotificationsQueryDTO>>> GetMyAsync(int userId, GetNotificationsQueryDTO query)
    {
        try
        {
            query.SetDefaultValueToPage();

            var repo = _uow.GetRepo<SystemNotification>();
            var baseQuery = repo.Get(new QueryOptions<SystemNotification>
            {
                Predicate = x => x.UserId == userId,
                Tracked = false
            });

            if (query.IsRead.HasValue) baseQuery = baseQuery.Where(x => x.IsRead == query.IsRead.Value);
            if (query.Type.HasValue) baseQuery = baseQuery.Where(x => x.Type == query.Type.Value);
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var k = query.Keyword.Trim();
                baseQuery = baseQuery.Where(x => x.Title.Contains(k) || x.Message.Contains(k));
            }
            if (query.From.HasValue) baseQuery = baseQuery.Where(x => x.CreatedAt >= query.From.Value);
            if (query.To.HasValue) baseQuery = baseQuery.Where(x => x.CreatedAt <= query.To.Value);

            var total = await baseQuery.CountAsync();
            query.TotalRecord = total;

            var items = await baseQuery
                .OrderByDescending(x => x.CreatedAt)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new NotificationResponseDTO
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Title = x.Title,
                    Message = x.Message,
                    Type = x.Type,
                    RelatedEntityType = x.RelatedEntityType,
                    RelatedEntityId = x.RelatedEntityId,
                    IsRead = x.IsRead,
                    CreatedAt = x.CreatedAt,
                    ReadAt = x.ReadAt
                })
                .ToListAsync();

            return new BaseResponseModel<PagingDataModel<NotificationResponseDTO, GetNotificationsQueryDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "OK",
                Data = new PagingDataModel<NotificationResponseDTO, GetNotificationsQueryDTO>(items, query)
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<PagingDataModel<NotificationResponseDTO, GetNotificationsQueryDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<object>> CountUnreadAsync(int userId)
    {
        try
        {
            var repo = _uow.GetRepo<SystemNotification>();
            var count = await repo.Get(new QueryOptions<SystemNotification>
            {
                Predicate = x => x.UserId == userId && !x.IsRead
            }).CountAsync();

            return new BaseResponseModel<object>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = count,
                Message = "OK"
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<object>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel> MarkAsReadAsync(int userId, int notificationId)
    {
        try
        {
            var repo = _uow.GetRepo<SystemNotification>();
            var entity = await repo.GetSingleAsync(new QueryOptions<SystemNotification>
            {
                Predicate = x => x.Id == notificationId && x.UserId == userId
            });
            if (entity is null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy thông báo"
                };
            }

            if (!entity.IsRead)
            {
                entity.IsRead = true;
                entity.ReadAt = DateTime.Now;

                await repo.UpdateAsync(entity);
                var save = await _uow.SaveAsync();
                if (!save.IsSuccess)
                {
                    return new BaseResponseModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = save.Message
                    };
                }

                var unreadCount = await repo.Get(new QueryOptions<SystemNotification>
                {
                    Predicate = x => x.UserId == userId && !x.IsRead
                }).CountAsync();

                await _broadcaster.SendToUserAsync(userId, NotificationMethod.NotificationMarkedAsRead.ToString(), notificationId);
                await _broadcaster.SendToUserAsync(userId, NotificationMethod.NotificationUnreadCount.ToString(), unreadCount);
            }

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "OK"
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel> MarkAllAsReadAsync(int userId)
    {
        try
        {
            var repo = _uow.GetRepo<SystemNotification>();
            var query = repo.Get(new QueryOptions<SystemNotification>
            {
                Predicate = x => x.UserId == userId && !x.IsRead,
                Tracked = true
            });

            var list = await query.ToListAsync();
            foreach (var noti in list)
            {
                noti.IsRead = true;
                noti.ReadAt = DateTime.Now;
            }

            var save = await _uow.SaveAsync();
            if (!save.IsSuccess)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = save.Message
                };
            }

            await _broadcaster.SendToUserAsync(userId, NotificationMethod.NotificationMarkedAsRead.ToString(), new { });
            await _broadcaster.SendToUserAsync(userId, NotificationMethod.NotificationUnreadCount.ToString(), 0);

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "OK"
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }
}