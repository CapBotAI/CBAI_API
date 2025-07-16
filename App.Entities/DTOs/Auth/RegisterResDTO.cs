using System;
using App.Entities.Entities.Core;

namespace App.Entities.DTOs.Auth;

public class RegisterResDTO
{
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public List<string>? Roles { get; set; }

    public RegisterResDTO(User user)
    {
        Id = user.Id;
        Username = user.UserName;
        Email = user.Email;
        PhoneNumber = user.PhoneNumber;
        Roles = user.UserRoles.Select(r => r.Role.Name).ToList();
    }
}
