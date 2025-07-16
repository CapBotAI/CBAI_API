using System;
using System.ComponentModel.DataAnnotations;
using App.Commons;
using App.Commons.ResponseModel;
using App.Entities.Entities.Core;
using App.Entities.Enums;

namespace App.Entities.DTOs.Auth;

public class RegisterDTO
{
    /// <summary>
    /// Địa chỉ email của người dùng
    /// </summary>
    /// <example>user@example.com</example>
    [Required(ErrorMessage = ConstantModel.Required)]
    [Display(Name = "Email"), StringLength(255, ErrorMessage = ConstantModel.MaxlengthError)]
    [EmailAddress(ErrorMessage = ConstantModel.EmailAddressFormatError)]
    public string Email { get; set; } = null!;

    /// <summary>
    /// PhoneNumber
    /// </summary>
    [Required(ErrorMessage = ConstantModel.Required)]
    [StringLength(11, ErrorMessage = "Số điện thoại chỉ chứa tối đa 11 số!")]
    public string PhoneNumber { get; set; } = null!;

    /// <summary>
    /// username
    /// </summary>
    [Required(ErrorMessage = ConstantModel.Required)]
    [Display(Name = "UserName"), StringLength(255, ErrorMessage = ConstantModel.MaxlengthError)]
    public string UserName { get; set; } = null!;

    /// <summary>
    /// Mật khẩu (tối thiểu 6 ký tự)
    /// </summary>
    /// <example>SecurePass123</example>
    [Required(ErrorMessage = ConstantModel.Required)]
    [Display(Name = "Mật khẩu"),
     StringLength(255, ErrorMessage = ConstantModel.PasswordStringLengthError, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [RegularExpression(ConstantModel.REGEX_PASSWORD, ErrorMessage = ConstantModel.PasswordInvalidFormat)]
    public string Password { get; set; } = null!;

    /// <summary>
    /// nhập lại mật khẩu
    /// </summary>
    [DataType(DataType.Password)]
    [Required(ErrorMessage = ConstantModel.Required)]
    [Compare("Password", ErrorMessage = ConstantModel.ConfirmPasswordError)]
    [Display(Name = "Xác nhận mật khẩu"), StringLength(255, ErrorMessage = ConstantModel.MaxlengthError)]
    public string ConfirmPassword { get; set; } = null!;


    /// <summary>
    /// Role của người dùng
    /// </summary>
    [Required(ErrorMessage = ConstantModel.Required)]
    public SystemRoles Role { get; set; }

    public BaseResponseModel IsModelValid(string option)
    {
        if (option == "Role")
        {
            if (!Enum.IsDefined(typeof(SystemRoles), Role))
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    Message = "Role không hợp lệ."
                };
            }
        }

        if (Password != ConfirmPassword)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Mật khẩu xác nhận không khớp"
            };
        }

        if (Role == SystemRoles.Administrator)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Không thể đăng ký tài khoản admin"
            };
        }

        return new BaseResponseModel
        {
            IsSuccess = true,
            Message = ""
        };
    }
}
