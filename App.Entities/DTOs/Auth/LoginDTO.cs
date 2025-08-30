using System.ComponentModel.DataAnnotations;
using App.Commons;
using App.Entities.Entities.Core;
using App.Entities.Enums;

namespace App.Entities.DTOs.Auth;

public class LoginDTO
{
    [Required(ErrorMessage = ConstantModel.Required)]
    public string EmailOrUsername { get; set; } = null!;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Role is required")]
    public SystemRoles Role { get; set; }
}
