using System;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Auth;
using App.Entities.Entities.Core;
using Microsoft.AspNetCore.Http;

namespace App.BLL.Implementations;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IIdentityRepository _identityRepository;


    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService, IIdentityRepository identityRepository)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _identityRepository = identityRepository;

    }

    public async Task<BaseResponseModel<LoginResponseDTOV2>> SignInAsyncV2(LoginDTO loginDTO)
    {
        throw new Exception("Not implement");
    }

    public async Task<BaseResponseModel<RegisterResDTO>> SignUpAsync(RegisterDTO dto)
    {
        try
        {

            var existedEmail = await _identityRepository.GetByEmailOrUserNameAsync(dto.Email);
            if (existedEmail != null)
            {
                return new BaseResponseModel<RegisterResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Email đã tồn tại trong hệ thống"
                };
            }

            // Check username exists
            var existedUserName = await _identityRepository.GetByEmailOrUserNameAsync(dto.UserName);
            if (existedUserName != null)
            {
                return new BaseResponseModel<RegisterResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Tên đăng nhập đã tồn tại trong hệ thống"
                };
            }

            var user = new User
            {
                Email = dto.Email,
                UserName = dto.UserName,
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = true,
                CreatedAt = DateTime.Now,
            };

            var result = await _identityRepository.AddUserAsync(user, dto.Password, dto.Role.ToString());
            if (!result.IsSuccess)
            {
                return new BaseResponseModel<RegisterResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = result.Message
                };
            }

            var createdUser = await _identityRepository.GetByEmailOrUserNameAsync(dto.Email);
            if (createdUser == null)
            {
                return new BaseResponseModel<RegisterResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Không thể tạo tài khoản người dùng."
                };
            }

            var userRoles = await _identityRepository.GetRolesAsync(createdUser.Id);
            var userData = new RegisterResDTO(createdUser);

            return new BaseResponseModel<RegisterResDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Đăng ký thành công",
                Data = userData
            };
        }
        catch (Exception)
        {
            throw;
        }
    }
}
