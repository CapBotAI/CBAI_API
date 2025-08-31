// App.BLL/Interfaces/IUserProfileService.cs
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.Entities.DTOs.UserProfiles;

namespace App.BLL.Interfaces;

public interface IUserProfileService
{
    Task<BaseResponseModel<UserProfileResponseDTO>> CreateAsync(CreateUserProfileDTO dto, int userId, bool isAdmin);
    Task<BaseResponseModel<UserProfileResponseDTO>> UpdateAsync(UpdateUserProfileDTO dto, int userId, bool isAdmin);
    Task<BaseResponseModel> DeleteAsync(int id, int userId, bool isAdmin);
    Task<BaseResponseModel<UserProfileResponseDTO>> GetByIdAsync(int id);
    Task<BaseResponseModel<UserProfileResponseDTO>> GetByUserIdAsync(int targetUserId);
    Task<BaseResponseModel<UserProfileResponseDTO>> GetMyProfileAsync(int userId);
}