using System;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Accounts;

namespace App.BLL.Interfaces;

public interface IAccountService
{
    Task<BaseResponseModel<List<RoleOverviewDTO>>> GetAllUserRoles(long userId);
}
