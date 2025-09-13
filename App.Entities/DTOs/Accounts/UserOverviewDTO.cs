using App.Entities.Entities.Core;
using App.Entities.Enums;

namespace App.Entities.DTOs.Accounts;

public class UserOverviewDTO
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }

    public List<RoleInUserOverviewDTO> RoleInUserOverviewDTOs { get; set; }

    public DateTime? CreatedAt { get; set; }

    public UserOverviewDTO(User user, List<Role> roles)
    {
        RoleInUserOverviewDTOs = roles.Select(r => new RoleInUserOverviewDTO(r)).ToList();

        Id = user.Id;
        Email = user.Email;
        UserName = user.UserName;
        PhoneNumber = user.PhoneNumber;
        CreatedAt = user.CreatedAt;
    }

}

public class RoleInUserOverviewDTO
{
    public int Id { get; set; }
    public string? Name { get; set; }

    public RoleInUserOverviewDTO(Role role)
    {
        Id = role.Id;
        Name = role.Name;
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