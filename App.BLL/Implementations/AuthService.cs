using System;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Auth;
using App.Entities.Entities_2;

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
        try
        {
            var accountRepo = _unitOfWork.GetRepo<SystemAccount>();
            var account = await accountRepo.GetSingleAsync(new QueryBuilder<SystemAccount>()
                        .WithPredicate(x => x.Email == loginDTO.Email && x.IsActive == true)
                        .Build());

            if (account == null)
            {
                return new BaseResponseModel<LoginResponseDTOV2>
                {
                    IsSuccess = false,
                    Message = "Invalid email or password"
                };
            }

            if (account.Password != loginDTO.Password)
            {
                return new BaseResponseModel<LoginResponseDTOV2>
                {
                    IsSuccess = false,
                    Message = "Invalid email or password"
                };
            }

            var roleName = account.Role switch
            {
                1 => "administrator",
                2 => "moderator",
                3 => "developer",
                4 => "member",
                _ => "other"
            };

            if (account.Role != 1 && account.Role != 2 && account.Role != 3 && account.Role != 4)
            {
                return new BaseResponseModel<LoginResponseDTOV2>
                {
                    IsSuccess = false,
                    Message = "No token issued for this role"
                };
            }

            var token = await _jwtService.GenerateTokenAsync(account);

            var response = new LoginResponseDTOV2
            {
                Token = token.AccessToken,
                Role = roleName
            };

            return new BaseResponseModel<LoginResponseDTOV2>
            {
                IsSuccess = true,
                Message = "Authentication successful",
                Data = response
            };
        }
        catch (Exception)
        {
            return new BaseResponseModel<LoginResponseDTOV2>
            {
                IsSuccess = false,
                Message = "Invalid email or password"
            };
        }
    }
}
