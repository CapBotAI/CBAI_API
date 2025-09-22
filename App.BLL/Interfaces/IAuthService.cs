using System;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Auth;

namespace App.BLL.Interfaces;

public interface IAuthService
{
    Task<BaseResponseModel<RegisterResDTO>> SignUpAsync(RegisterDTO dto);
    Task<BaseResponseModel<LoginResponseDTO>> SignInAsync(LoginDTO loginDTO);
    Task<BaseResponseModel<object>> ChangePasswordAsync(ChangePasswordDTO dto, string userId);
    Task<BaseResponseModel<object>> ForgotPasswordAsync(ForgotPasswordRequestDTO dto);
    Task<BaseResponseModel<object>> ResetPasswordAsync(ResetPasswordDTO dto);
}
