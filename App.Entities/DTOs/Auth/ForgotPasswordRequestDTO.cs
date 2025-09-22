using System.ComponentModel.DataAnnotations;
using App.Commons;

namespace App.Entities.DTOs.Auth;

public class ForgotPasswordRequestDTO
{
    [Required(ErrorMessage = ConstantModel.Required)]
    [EmailAddress(ErrorMessage = ConstantModel.EmailAddressFormatError)]
    public string Email { get; set; } = null!;
}
