using App.Entities.Entities.Core;
using App.Entities.Enums;

namespace App.Entities.DTOs.Accounts;

public class UserViewDTO
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }

    public UserViewDTO(User user)
    {
        Id = user.Id;
        Email = user.Email;
    }
}
