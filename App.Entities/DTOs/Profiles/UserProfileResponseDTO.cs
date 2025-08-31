// App.Entities/DTOs/UserProfiles/UserProfileResponseDTO.cs
using App.Entities.Entities.App;

namespace App.Entities.DTOs.UserProfiles;

public class UserProfileResponseDTO
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Address { get; set; }
    public string? Avatar { get; set; }
    public string? CoverImage { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    public UserProfileResponseDTO(UserProfile entity)
    {
        Id = entity.Id;
        UserId = entity.UserId;
        FullName = entity.FullName;
        Address = entity.Address;
        Avatar = entity.Avatar;
        CoverImage = entity.CoverImage;
        CreatedAt = entity.CreatedAt;
        CreatedBy = entity.CreatedBy;
        LastModifiedAt = entity.LastModifiedAt;
        LastModifiedBy = entity.LastModifiedBy;
    }
}