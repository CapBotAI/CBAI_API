// App.BLL/Implementations/UserProfileService.cs
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.UserProfiles;
using App.Entities.Entities.App;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Implementations;

public class UserProfileService : IUserProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityRepository _identityRepository;

    public UserProfileService(IUnitOfWork unitOfWork, IIdentityRepository identityRepository)
    {
        _unitOfWork = unitOfWork;
        _identityRepository = identityRepository;
    }

    public async Task<BaseResponseModel<UserProfileResponseDTO>> CreateAsync(CreateUserProfileDTO dto, int userId, bool isAdmin)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync(userId);
            if (user is null)
                return new BaseResponseModel<UserProfileResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };

            var repo = _unitOfWork.GetRepo<UserProfile>();

            int targetUserId = dto.UserId.HasValue && dto.UserId.Value > 0 ? dto.UserId.Value : userId;
            if (!isAdmin && targetUserId != userId)
            {
                return new BaseResponseModel<UserProfileResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền tạo hồ sơ cho người dùng khác"
                };
            }

            var existed = await repo.GetSingleAsync(new QueryOptions<UserProfile>
            {
                Predicate = x => x.IsActive && x.DeletedAt == null && x.UserId == targetUserId,
                Tracked = false
            });
            if (existed != null)
            {
                return new BaseResponseModel<UserProfileResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Hồ sơ đã tồn tại cho người dùng này"
                };
            }

            var entity = dto.GetEntity();
            entity.UserId = targetUserId;
            entity.CreatedBy = user.UserName;
            entity.CreatedAt = DateTime.Now;

            await repo.CreateAsync(entity);
            var save = await _unitOfWork.SaveAsync();
            if (!save.IsSuccess)
            {
                return new BaseResponseModel<UserProfileResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = save.Message
                };
            }

            return new BaseResponseModel<UserProfileResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo hồ sơ thành công",
                Data = new UserProfileResponseDTO(entity)
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<UserProfileResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<UserProfileResponseDTO>> UpdateAsync(UpdateUserProfileDTO dto, int userId, bool isAdmin)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync(userId);
            if (user is null)
                return new BaseResponseModel<UserProfileResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };

            var repo = _unitOfWork.GetRepo<UserProfile>();
            var entity = await repo.GetSingleAsync(new QueryOptions<UserProfile>
            {
                Predicate = x => x.Id == dto.Id && x.IsActive && x.DeletedAt == null,
                Tracked = false
            });

            if (entity == null)
            {
                return new BaseResponseModel<UserProfileResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Hồ sơ chưa được tạo, hãy cập nhật hồ sơ."
                };
            }

            if (!isAdmin && entity.UserId != userId)
            {
                return new BaseResponseModel<UserProfileResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền cập nhật hồ sơ này"
                };
            }

            entity.FullName = dto.FullName;
            entity.Address = dto.Address;
            entity.Avatar = dto.Avatar;
            entity.CoverImage = dto.CoverImage;
            entity.LastModifiedBy = user.UserName;
            entity.LastModifiedAt = DateTime.Now;

            await repo.UpdateAsync(entity);
            var save = await _unitOfWork.SaveAsync();
            if (!save.IsSuccess)
            {
                return new BaseResponseModel<UserProfileResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = save.Message
                };
            }

            return new BaseResponseModel<UserProfileResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật hồ sơ thành công",
                Data = new UserProfileResponseDTO(entity)
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<UserProfileResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel> DeleteAsync(int id, int userId, bool isAdmin)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync(userId);
            if (user is null)
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };

            var repo = _unitOfWork.GetRepo<UserProfile>();
            var entity = await repo.GetSingleAsync(new QueryOptions<UserProfile>
            {
                Predicate = x => x.Id == id && x.IsActive && x.DeletedAt == null,
                Tracked = false
            });

            if (entity == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Hồ sơ chưa được tạo, hãy cập nhật hồ sơ."
                };
            }

            if (!isAdmin && entity.UserId != userId)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền xóa hồ sơ này"
                };
            }

            entity.IsActive = false;
            entity.DeletedAt = DateTime.Now;
            entity.LastModifiedBy = user.UserName;

            await repo.UpdateAsync(entity);
            var save = await _unitOfWork.SaveAsync();
            if (!save.IsSuccess)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = save.Message
                };
            }

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa hồ sơ thành công"
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

    public async Task<BaseResponseModel<UserProfileResponseDTO>> GetByIdAsync(int id)
    {
        try
        {
            var repo = _unitOfWork.GetRepo<UserProfile>();
            var entity = await repo.GetSingleAsync(new QueryOptions<UserProfile>
            {
                Predicate = x => x.Id == id && x.IsActive && x.DeletedAt == null,
                Tracked = false
            });

            if (entity == null)
            {
                return new BaseResponseModel<UserProfileResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Hồ sơ chưa được tạo, hãy cập nhật hồ sơ."
                };
            }

            return new BaseResponseModel<UserProfileResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = new UserProfileResponseDTO(entity)
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<UserProfileResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<UserProfileResponseDTO>> GetByUserIdAsync(int targetUserId)
    {
        try
        {
            var repo = _unitOfWork.GetRepo<UserProfile>();
            var entity = await repo.GetSingleAsync(new QueryOptions<UserProfile>
            {
                Predicate = x => x.UserId == targetUserId && x.IsActive && x.DeletedAt == null && x.DeletedAt == null,
                Tracked = false
            });

            if (entity == null)
            {
                return new BaseResponseModel<UserProfileResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Hồ sơ chưa được tạo, hãy cập nhật hồ sơ."
                };
            }

            return new BaseResponseModel<UserProfileResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = new UserProfileResponseDTO(entity)
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<UserProfileResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public Task<BaseResponseModel<UserProfileResponseDTO>> GetMyProfileAsync(int userId)
    {
        return GetByUserIdAsync(userId);
    }
}