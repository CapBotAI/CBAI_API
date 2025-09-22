using System;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Auth;
using App.Entities.Entities.Core;
using Microsoft.AspNetCore.Http;
using App.Commons.Interfaces; 
using System.Collections.Generic;
using App.Commons; 

namespace App.BLL.Implementations;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IIdentityRepository _identityRepository;
    private readonly IEmailService _emailService; 


    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService, IIdentityRepository identityRepository, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _identityRepository = identityRepository;
        _emailService = emailService; 
    }

    public async Task<BaseResponseModel<LoginResponseDTO>> SignInAsync(LoginDTO loginDTO)
    {
        try
        {
            var user = await _identityRepository.GetByEmailOrUserNameAsync(loginDTO.EmailOrUsername);
            if (user == null)
            {
                return new BaseResponseModel<LoginResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "Email hoặc username không tồn tại trong hệ thống"
                };
            }

            if (user.DeletedAt != null)
            {
                return new BaseResponseModel<LoginResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "Tài khoản đã bị vô hiệu hóa"
                };
            }

            var userRoles = await _identityRepository.GetRolesAsync(user.Id);
            if (!userRoles.Contains(loginDTO.Role.ToString()))
            {
                return new BaseResponseModel<LoginResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "Người dùng không có quyền truy cập"
                };
            }

            var checkPassword = await _identityRepository.CheckPasswordAsync(user, loginDTO.Password);
            if (!checkPassword)
            {
                return new BaseResponseModel<LoginResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "Mật khẩu không chính xác"
                };
            }

            var token = await _jwtService.GenerateJwtTokenWithSpecificRole(user, loginDTO.Role.ToString());
            return new BaseResponseModel<LoginResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Đăng nhập thành công",
                Data = new LoginResponseDTO { TokenData = token }
            };
        }
        catch (System.Exception)
        {
            throw;
        }
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

            // Send welcome email
            if (string.IsNullOrEmpty(createdUser.Email) || string.IsNullOrEmpty(createdUser.UserName))
            {
                return new BaseResponseModel<RegisterResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "User email or username is invalid."
                };
            }

            await SendWelcomeEmailAsync(createdUser.Email, createdUser.UserName, dto.Password, dto.Role.ToString());

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

    private async Task SendWelcomeEmailAsync(string email, string username, string password, string role)
    {
        var emailContent = @"<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Welcome to CapBot</title>
    <style>
        body { font-family: Arial, sans-serif; background-color: #f5f5f5; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 30px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); }
        .header { background-color: #6b21a8; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; }
        .content p { font-size: 14px; color: #4a2566; margin: 10px 0; }
        .btn { display: inline-block; padding: 12px 25px; background-color: #6b21a8; color: white; text-decoration: none; border-radius: 4px; font-size: 16px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Welcome to CapBot</h2>
        </div>
        <div class='content'>
            <p>Dear <strong>{username}</strong>,</p>
            <p>Your account has been successfully created. Below are your login details:</p>
            <p><strong>Email/Username:</strong> {email}</p>
            <p><strong>Password:</strong> {password}</p>
            <p><strong>Role:</strong> {role}</p>
            <p>We strongly recommend that you change your password after logging in for the first time.</p>
            <p>Thank you,<br />CAPBOT Team</p>
        </div>
    </div>
</body>
</html>";

        emailContent = emailContent.Replace("{username}", username)
                                   .Replace("{email}", email)
                                   .Replace("{password}", password)
                                   .Replace("{role}", role);

        // Assuming _emailService is injected and configured to send emails
        var emailModel = new EmailModel(new List<string> { email }, "Welcome to CapBot", emailContent);
        await _emailService.SendEmailAsync(emailModel);
    }

    public async Task<BaseResponseModel<object>> ChangePasswordAsync(ChangePasswordDTO dto, string userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync(long.Parse(userId));
            if (user == null)
            {
                return new BaseResponseModel<object>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "User not found."
                };
            }

            var isOldPasswordValid = await _identityRepository.CheckPasswordAsync(user, dto.OldPassword);
            if (!isOldPasswordValid)
            {
                return new BaseResponseModel<object>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Old password is incorrect."
                };
            }

            var result = await _identityRepository.ChangePasswordAsync(user, dto.NewPassword);
            if (!result)
            {
                return new BaseResponseModel<object>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Failed to change password."
                };
            }

            return new BaseResponseModel<object>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Password changed successfully."
            };
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while changing the password.", ex);
        }
    }

    public async Task<BaseResponseModel<object>> ForgotPasswordAsync(ForgotPasswordRequestDTO dto)
    {
        try
        {
            var user = await _identityRepository.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                return new BaseResponseModel<object>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Email không tồn tại trong hệ thống"
                };
            }

            // Generate password reset token
            var token = await _identityRepository.GeneratePasswordResetTokenAsync(user);

                // build reset link pointing to the API GET handler so clicking from Swagger/browser reaches the form
                var encodedToken = System.Net.WebUtility.UrlEncode(token);
                var resetLink = $"/api/auth/reset-password?userId={user.Id}&token={encodedToken}";

                // send email with clear HTML + plain-text content showing userId and token
                var emailBody = $@"<html>
    <body style='font-family: Arial, sans-serif; color:#222'>
      <h2>Yêu cầu đặt lại mật khẩu</h2>
      <p>Xin chào <strong>{System.Net.WebUtility.HtmlEncode(user.UserName ?? "")}</strong>,</p>
      <p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản của mình. Bạn có thể sử dụng đường dẫn bên dưới để đặt lại mật khẩu (mở trong trình duyệt hoặc trong Swagger):</p>
      <p><a href='{resetLink}'>{resetLink}</a></p>
      <p>Hoặc sao chép thông tin sau nếu cần thực hiện thủ công:</p>
      <pre style='background:#f6f6f6;padding:10px;border-radius:4px'>UserId: {user.Id}
    Token: {token}</pre>
      <p>Lưu ý: token có hiệu lực trong thời gian ngắn. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
      <p>Trân trọng,<br/>CAPBOT Team</p>
    </body>
    </html>";

                        // Plain-text fallback (some mail clients may strip HTML)
                        var textBody = $"Xin chào {user.UserName},\n\n" +
                                                     "Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Dùng đường dẫn sau để đặt lại mật khẩu:\n\n" +
                                                     resetLink + "\n\n" +
                                                     $"UserId: {user.Id}\nToken: {token}\n\n" +
                                                     "Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.\n\nTrân trọng,\nCAPBOT";

                        var recipients = new List<string>();
                        if (!string.IsNullOrWhiteSpace(user.Email)) recipients.Add(user.Email);
                        // EmailModel will set both BodyPlainText and BodyHtml to the provided body; pass HTML which is preferred
                        var emailModel = new EmailModel(recipients, "Yêu cầu đặt lại mật khẩu", emailBody);
                        await _emailService.SendEmailAsync(emailModel);

            return new BaseResponseModel<object>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Yêu cầu đặt lại mật khẩu đã được gửi đến email nếu email tồn tại"
            };
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while processing forgot password.", ex);
        }
    }

    public async Task<BaseResponseModel<object>> ResetPasswordAsync(ResetPasswordDTO dto)
    {
        try
        {
            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return new BaseResponseModel<object>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status422UnprocessableEntity,
                    Message = "Mật khẩu xác nhận không khớp"
                };
            }

            var user = await _identityRepository.GetByIdAsync(long.Parse(dto.UserId));
            if (user == null)
            {
                return new BaseResponseModel<object>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "User not found"
                };
            }

            var result = await _identityRepository.ResetPasswordAsync(dto.UserId, dto.Token, dto.NewPassword);
            if (!result)
            {
                return new BaseResponseModel<object>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Không thể đặt lại mật khẩu. Token có thể không hợp lệ hoặc đã hết hạn"
                };
            }

            return new BaseResponseModel<object>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Đặt lại mật khẩu thành công"
            };
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while resetting the password.", ex);
        }
    }
}