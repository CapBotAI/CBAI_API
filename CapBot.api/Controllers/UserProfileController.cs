// CapBot.api/Controllers/UserProfileController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;
using App.BLL.Interfaces;
using App.Commons.BaseAPI;
using App.Entities.DTOs.UserProfiles;

namespace CapBot.api.Controllers
{
    [Route("api/user-profiles")]
    [ApiController]
    [Authorize]
    public class UserProfileController : BaseAPIController
    {
        private readonly IUserProfileService _service;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(IUserProfileService service, ILogger<UserProfileController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Tạo hồ sơ người dùng")]
        [SwaggerResponse(201, "Tạo hồ sơ thành công")]
        [SwaggerResponse(403, "Không có quyền")]
        [SwaggerResponse(409, "Hồ sơ đã tồn tại")]
        public async Task<IActionResult> Create([FromBody] CreateUserProfileDTO dto)
        {
            if (!ModelState.IsValid) return ModelInvalid();
            try
            {
                var result = await _service.CreateAsync(dto, UserId, IsAdmin);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Create UserProfile for user {UserId}", UserId);
                return Error("Đã xảy ra lỗi!");
            }
        }

        [HttpPut]
        [SwaggerOperation(Summary = "Cập nhật hồ sơ")]
        [SwaggerResponse(200, "Cập nhật thành công")]
        [SwaggerResponse(403, "Không có quyền")]
        [SwaggerResponse(404, "Không tìm thấy")]
        public async Task<IActionResult> Update([FromBody] UpdateUserProfileDTO dto)
        {
            if (!ModelState.IsValid) return ModelInvalid();
            try
            {
                var result = await _service.UpdateAsync(dto, UserId, IsAdmin);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Update UserProfile {Id} by user {UserId}", dto.Id, UserId);
                return Error("Đã xảy ra lỗi!");
            }
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Xóa hồ sơ (soft delete)")]
        [SwaggerResponse(200, "Xóa thành công")]
        [SwaggerResponse(403, "Không có quyền")]
        [SwaggerResponse(404, "Không tìm thấy")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id, UserId, IsAdmin);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Delete UserProfile {Id} by user {UserId}", id, UserId);
                return Error("Đã xảy ra lỗi!");
            }
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Lấy hồ sơ theo ID")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Get UserProfile by Id {Id}", id);
                return Error("Đã xảy ra lỗi!");
            }
        }

        [HttpGet("by-user/{userId}")]
        [SwaggerOperation(Summary = "Lấy hồ sơ theo UserId")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            try
            {
                var result = await _service.GetByUserIdAsync(userId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Get UserProfile by userId {UserId}", userId);
                return Error("Đã xảy ra lỗi!");
            }
        }

        [HttpGet("me")]
        [SwaggerOperation(Summary = "Lấy hồ sơ của chính tôi")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var result = await _service.GetMyProfileAsync(UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Get My UserProfile for user {UserId}", UserId);
                return Error("Đã xảy ra lỗi!");
            }
        }
    }
}