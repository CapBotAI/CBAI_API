using System;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.Entities.DTOs.Accounts;
using Microsoft.AspNetCore.Http;

namespace App.BLL.Implementations;

public class AccountService : IAccountService
{
    private readonly IIdentityRepository _identityRepository;

    public AccountService(IIdentityRepository identityRepository)
    {
        _identityRepository = identityRepository;
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
            var loggedUser = await _identityRepository.GetByIdAsync(loggedUserId);
            if (loggedUser == null) return new BaseResponseModel<UserDetailDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                Message = "Tài khoản đang đăng nhập không tồn tại.",
                Data = null
            };


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
}