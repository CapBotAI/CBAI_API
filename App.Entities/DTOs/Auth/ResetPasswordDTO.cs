using System.ComponentModel.DataAnnotations;
using App.Commons;

namespace App.Entities.DTOs.Auth;

public class ResetPasswordDTO
{
    [Required(ErrorMessage = ConstantModel.Required)]
    public string UserId { get; set; } = null!;

    [Required(ErrorMessage = ConstantModel.Required)]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = ConstantModel.Required)]
    [StringLength(255, ErrorMessage = ConstantModel.PasswordStringLengthError, MinimumLength = 6)]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = ConstantModel.Required)]
    [Compare("NewPassword", ErrorMessage = ConstantModel.ConfirmPasswordError)]
    public string ConfirmPassword { get; set; } = null!;
}
