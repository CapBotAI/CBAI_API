using System;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Auth;

namespace App.BLL.Interfaces;

public interface IAuthService
{
    Task<BaseResponseModel<RegisterResDTO>> SignUpAsync(RegisterDTO dto);
    Task<BaseResponseModel<LoginResponseDTO>> SignInAsync(LoginDTO loginDTO);
}
