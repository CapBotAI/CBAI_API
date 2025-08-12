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
}