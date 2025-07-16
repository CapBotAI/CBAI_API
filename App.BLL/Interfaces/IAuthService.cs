using System;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Auth;

namespace App.BLL.Interfaces;

public interface IAuthService
{
    Task<BaseResponseModel<LoginResponseDTOV2>> SignInAsyncV2(LoginDTO loginDTO);
}
