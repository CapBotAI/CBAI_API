using System;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Auth;
using App.Entities.Entities.Core;

namespace App.BLL.Implementations;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<BaseResponseModel<LoginResponseDTOV2>> SignInAsyncV2(LoginDTO loginDTO)
    {
        throw new Exception("Not implement");
    }
}
