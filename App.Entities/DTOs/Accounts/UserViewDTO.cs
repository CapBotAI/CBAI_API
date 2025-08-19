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

public class UserDetailDTO
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public List<string>? Role { get; set; }
    public string? Username { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserDetailDTO(User user, List<string>? roles)
    {
        Id = user.Id;
        Email = user.Email;
        Role = roles;
        Username = user.UserName;
        CreatedAt = user.CreatedAt;
    }
}