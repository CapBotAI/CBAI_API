// App.Entities/DTOs/UserProfiles/CreateUserProfileDTO.cs
using System.ComponentModel.DataAnnotations;
using App.Commons;
using App.Entities.Entities.App;
using FS.Commons.Interfaces;

namespace App.Entities.DTOs.UserProfiles;

public class CreateUserProfileDTO : IEntity<UserProfile>
{
    public int? UserId { get; set; }

    [StringLength(255, ErrorMessage = "Họ tên không được vượt quá 255 ký tự")]
    public string? FullName { get; set; }

    [StringLength(512, ErrorMessage = "Địa chỉ không được vượt quá 512 ký tự")]
    public string? Address { get; set; }

    [StringLength(1024, ErrorMessage = "Avatar URL quá dài")]
    public string? Avatar { get; set; }

    [StringLength(1024, ErrorMessage = "CoverImage URL quá dài")]
    public string? CoverImage { get; set; }

    public UserProfile GetEntity()
    {
        return new UserProfile
        {
            FullName = FullName,
            Address = Address,
            Avatar = Avatar,
            CoverImage = CoverImage,
            IsActive = true,
            DeletedAt = null
        };
    }
}