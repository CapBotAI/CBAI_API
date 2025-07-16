using System.ComponentModel.DataAnnotations;
using App.Commons;

namespace App.Entities.DTOs.Auth;

public class LoginDTO
{
    [Required(ErrorMessage = ConstantModel.Required)]
    [EmailAddress(ErrorMessage = ConstantModel.EmailAddressFormatError)]
    public string EmailOrUsername { get; set; } = null!;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = null!;
}
