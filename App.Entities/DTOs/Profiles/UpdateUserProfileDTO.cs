// App.Entities/DTOs/UserProfiles/UpdateUserProfileDTO.cs
using System.ComponentModel.DataAnnotations;

namespace App.Entities.DTOs.UserProfiles;

public class UpdateUserProfileDTO
{
    [Required(ErrorMessage = "Id hồ sơ là bắt buộc")]
    public int Id { get; set; }

    [StringLength(255, ErrorMessage = "Họ tên không được vượt quá 255 ký tự")]
    public string? FullName { get; set; }

    [StringLength(512, ErrorMessage = "Địa chỉ không được vượt quá 512 ký tự")]
    public string? Address { get; set; }

    [StringLength(1024, ErrorMessage = "Avatar URL quá dài")]
    public string? Avatar { get; set; }

    [StringLength(1024, ErrorMessage = "CoverImage URL quá dài")]
    public string? CoverImage { get; set; }
}