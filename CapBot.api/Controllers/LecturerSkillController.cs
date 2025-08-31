// CapBot.api/Controllers/LecturerSkillController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;
using App.BLL.Interfaces;
using App.Commons.BaseAPI;
using App.Commons;
using App.Commons.Paging;
using App.Entities.DTOs.LecturerSkills;

namespace CapBot.api.Controllers
{
    [Route("api/lecturer-skills")]
    [ApiController]
    [Authorize]
    public class LecturerSkillController : BaseAPIController
    {
        private readonly ILecturerSkillService _service;
        private readonly ILogger<LecturerSkillController> _logger;

        public LecturerSkillController(ILecturerSkillService service, ILogger<LecturerSkillController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Tạo kỹ năng cho giảng viên")]
        [SwaggerResponse(201, "Tạo kỹ năng thành công")]
        [SwaggerResponse(403, "Không có quyền")]
        [SwaggerResponse(409, "Kỹ năng trùng")]
        public async Task<IActionResult> Create([FromBody] CreateLecturerSkillDTO dto)
        {
            if (!ModelState.IsValid) return ModelInvalid();
            try
            {
                var result = await _service.CreateAsync(dto, UserId, IsAdmin);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Create LecturerSkill for user {UserId}", UserId);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        [HttpPut]
        [SwaggerOperation(Summary = "Cập nhật kỹ năng")]
        [SwaggerResponse(200, "Cập nhật thành công")]
        [SwaggerResponse(403, "Không có quyền")]
        [SwaggerResponse(404, "Không tìm thấy")]
        [SwaggerResponse(409, "Kỹ năng trùng")]
        public async Task<IActionResult> Update([FromBody] UpdateLecturerSkillDTO dto)
        {
            if (!ModelState.IsValid) return ModelInvalid();
            try
            {
                var result = await _service.UpdateAsync(dto, UserId, IsAdmin);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Update LecturerSkill {Id} by user {UserId}", dto.Id, UserId);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Xóa kỹ năng (soft delete)")]
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
                _logger.LogError(ex, "Error Delete LecturerSkill {Id} by user {UserId}", id, UserId);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Lấy kỹ năng theo ID")]
        [SwaggerResponse(200, "Thành công")]
        [SwaggerResponse(404, "Không tìm thấy")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Get LecturerSkill {Id}", id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Lấy danh sách kỹ năng theo giảng viên (phân trang)")]
        [SwaggerResponse(200, "Thành công")]
        public async Task<IActionResult> GetByLecturer([FromQuery] int lecturerId, [FromQuery] GetLecturerSkillQueryDTO dto)
        {
            try
            {
                var result = await _service.GetByLecturerAsync(lecturerId, dto);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Get skills by lecturer {LecturerId}", lecturerId);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        [HttpGet("me")]
        [SwaggerOperation(Summary = "Lấy danh sách kỹ năng của chính mình (phân trang)")]
        [SwaggerResponse(200, "Thành công")]
        public async Task<IActionResult> GetMySkills([FromQuery] GetLecturerSkillQueryDTO dto)
        {
            try
            {
                var result = await _service.GetMySkillsAsync(UserId, dto);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Get my skills for user {UserId}", UserId);
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}