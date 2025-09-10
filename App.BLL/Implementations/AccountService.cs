using System;
using App.BLL.Interfaces;
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.Entities.Constants;
using App.Entities.DTOs.Accounts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace App.BLL.Implementations;

public class AccountService : IAccountService
{
    private readonly IIdentityRepository _identityRepository;
    private readonly IConfiguration _configuration;

    public AccountService(IIdentityRepository identityRepository, IConfiguration configuration)
    {
        _identityRepository = identityRepository;
        _configuration = configuration;
    }

    public async Task<BaseResponseModel<List<RoleOverviewDTO>>> GetAllUserRoles(long userId)
    {
        try
        {
            var roles = await _identityRepository.GetUserRolesAsync(userId);
            if (roles != null)
            {
                return new BaseResponseModel<List<RoleOverviewDTO>>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Lấy danh sách quyền thành công",
                    Data = roles.Select(r => new RoleOverviewDTO(r)).ToList() ?? new List<RoleOverviewDTO>()
                };
            }
            return new BaseResponseModel<List<RoleOverviewDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status200OK,
                Message = "Danh sách role rỗng",
                Data = new List<RoleOverviewDTO>()
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<UserDetailDTO>> AddRoleToUser(long userId, List<string> roles, long loggedUserId)
    {
        try
        {

            if (roles == null || roles.Count == 0)
            {
                return new BaseResponseModel<UserDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Danh sách quyền không hợp lệ",
                    Data = null
                };
            }

            foreach (var role in roles)
            {
                var isRoleExist = await _identityRepository.IsRoleExist(role);
                if (!isRoleExist)
                {
                    return new BaseResponseModel<UserDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = $"Vai trò {role} không tồn tại. Chỉ có thể thêm Moderator, Supervisor, Reviewer.",
                        Data = null
                    };
                }
            }

            var user = await _identityRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new BaseResponseModel<UserDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại.",
                    Data = null
                };
            }

            var userRoles = await _identityRepository.GetUserRolesAsync(user.Id);
            if (userRoles != null && userRoles.Any(r => roles.Contains(r.Name)))
            {
                return new BaseResponseModel<UserDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Người dùng đã có vai trò này.",
                    Data = null
                };
            }

            var result = await _identityRepository.AddRolesToUserAsync(user, roles);
            if (!result)
            {
                return new BaseResponseModel<UserDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Thêm quyền cho người dùng thất bại.",
                    Data = null
                };
            }

            var reloadUserRoles = await _identityRepository.GetUserRolesAsync(userId);
            return new BaseResponseModel<UserDetailDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Thêm quyền cho người dùng thành công",
                Data = new UserDetailDTO(user!, reloadUserRoles.Select(r => r.Name).ToList()!)
            };
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<BaseResponseModel<UserDetailDTO>> RemoveRoleFromUserRoles(int userId, List<string> roles)
    {
        try
        {
            if (roles == null || roles.Count == 0)
            {
                return new BaseResponseModel<UserDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Danh sách quyền không hợp lệ",
                    Data = null
                };
            }

            foreach (var role in roles)
            {
                var isRoleExist = await _identityRepository.IsRoleExist(role);
                if (!isRoleExist)
                {
                    return new BaseResponseModel<UserDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = $"Vai trò {role} không tồn tại. Chỉ có thể chọn Moderator, Supervisor, Reviewer.",
                        Data = null
                    };
                }
            }

            var user = await _identityRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new BaseResponseModel<UserDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại.",
                    Data = null
                };
            }

            var userRoles = await _identityRepository.GetUserRolesAsync(user.Id);
            if (userRoles == null || userRoles.Count == 0)
            {
                return new BaseResponseModel<UserDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Người dùng không có vai trò nào.",
                    Data = null
                };
            }
            if (userRoles.Count == 1 && userRoles.Any(r => roles.Contains(r.Name)))
            {
                return new BaseResponseModel<UserDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Người dùng chỉ có một vai trò, không thể xóa.",
                    Data = null
                };
            }
            if (userRoles != null && (!userRoles.Any(r => roles.Contains(r.Name))))
            {
                return new BaseResponseModel<UserDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Người dùng không tồn tại vai trò này.",
                    Data = null
                };
            }

            var result = await _identityRepository.RemoveRolesFromUserAsync(user, roles);
            if (!result)
            {
                return new BaseResponseModel<UserDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Xóa quyền cho người dùng thất bại.",
                    Data = null
                };
            }

            var reloadUserRoles = await _identityRepository.GetUserRolesAsync(userId);
            return new BaseResponseModel<UserDetailDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa quyền cho người dùng thành công",
                Data = new UserDetailDTO(user!, reloadUserRoles.Select(r => r.Name).ToList()!)
            };
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<UserOverviewDTO, GetUsersQueryDTO>>> GetUsers(GetUsersQueryDTO query)
    {
        try
        {
            var users = await _identityRepository.GetAccounts(query);
            if (users == null || users.Count == 0) return new BaseResponseModel<PagingDataModel<UserOverviewDTO, GetUsersQueryDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Danh sách người dùng rỗng",
                Data = new PagingDataModel<UserOverviewDTO, GetUsersQueryDTO>(new List<UserOverviewDTO>(), query)
            };

            var userOverviews = new List<UserOverviewDTO>();
            foreach (var user in users)
            {
                var roles = await _identityRepository.GetUserRolesAsync(user.Id);
                userOverviews.Add(new UserOverviewDTO(user, roles));
            }
            return new BaseResponseModel<PagingDataModel<UserOverviewDTO, GetUsersQueryDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách người dùng thành công",
                Data = new PagingDataModel<UserOverviewDTO, GetUsersQueryDTO>(userOverviews, query)
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel> SoftDeleteUser(int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại.",
                };
            }

            if (user.UserName == SystemRoleConstants.Administrator || user.Email == _configuration["AdminAccount:Email"])
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Không thể xóa tài khoản admin.",
                };
            }

            user.DeletedAt = DateTime.Now;
            var result = await _identityRepository.UpdateAsync(user);

            if (!result)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Xóa người dùng thất bại.",
                };
            }

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa người dùng thành công.",
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

}