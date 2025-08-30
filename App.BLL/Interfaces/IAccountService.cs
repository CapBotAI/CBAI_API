using System;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Accounts;

namespace App.BLL.Interfaces;

public interface IAccountService
{
    Task<BaseResponseModel<List<RoleOverviewDTO>>> GetAllUserRoles(long userId);
    Task<BaseResponseModel<UserDetailDTO>> AddRoleToUser(long userId, List<string> roles, long loggedUserId);
}
