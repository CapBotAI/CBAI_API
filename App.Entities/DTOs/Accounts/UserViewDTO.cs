using App.Entities.Entities_2;
using App.Entities.Enums;

namespace App.Entities.DTOs.Accounts;

public class UserViewDTO
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }

    public UserViewDTO(SystemAccount user)
    {
        Id = user.AccountID;
        Email = user.Email;
        Role = user.Role switch
        {
            1 => nameof(UserRoles.Administrator),
            2 => nameof(UserRoles.Moderator),
            3 => nameof(UserRoles.Developer),
            4 => nameof(UserRoles.Member),
            _ => "Other"
        };
    }
}
