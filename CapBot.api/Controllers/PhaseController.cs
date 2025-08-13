using App.BLL.Interfaces;
using App.Commons;
using App.Commons.BaseAPI;
using App.Entities.Constants;
using App.Entities.DTOs.Phases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CapBot.api.Controllers
{
    [Route("api/phase")]
    [ApiController]
    public class PhaseController : BaseAPIController
    {
        private readonly IPhaseService _phaseService;
        private readonly ILogger<PhaseController> _logger;

        public PhaseController(IPhaseService phaseService, ILogger<PhaseController> logger)
        {
            _phaseService = phaseService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo giai đoạn mới
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpPost("create")]
        [SwaggerOperation(
            Summary = "Tạo giai đoạn mới",
            Description = "Tạo phase thuộc một semester với phase type và mốc thời gian")]
        [SwaggerResponse(201, "Tạo giai đoạn thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Semester hoặc PhaseType không tồn tại")]
        [SwaggerResponse(409, "Tên giai đoạn đã tồn tại trong học kỳ này")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Create([FromBody] CreatePhaseDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            var val = ((App.Commons.Interfaces.IValidationPipeline)dto).Validate();
            if (!val.IsSuccess)
            {
                return ProcessServiceResponse(val);
            }

            try
            {
                var result = await _phaseService.CreatePhase(dto, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating phase");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Cập nhật giai đoạn
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpPut("update")]
        [SwaggerOperation(
            Summary = "Cập nhật giai đoạn",
            Description = "Cập nhật thông tin phase (semester, phase type, thời gian, deadline)")]
        [SwaggerResponse(200, "Cập nhật giai đoạn thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Giai đoạn/Semester/PhaseType không tồn tại")]
        [SwaggerResponse(409, "Tên giai đoạn đã tồn tại trong học kỳ này")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Update([FromBody] UpdatePhaseDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            var val = ((App.Commons.Interfaces.IValidationPipeline)dto).Validate();
            if (!val.IsSuccess)
            {
                return ProcessServiceResponse(val);
            }

            try
            {
                var result = await _phaseService.UpdatePhase(dto, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating phase {PhaseId}", dto.Id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Xóa giai đoạn
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpDelete("delete/{id}")]
        [SwaggerOperation(
            Summary = "Xóa giai đoạn",
            Description = "Xóa (soft delete) một phase; không thể xóa nếu đang có submission sử dụng")]
        [SwaggerResponse(200, "Xóa giai đoạn thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Giai đoạn không tồn tại")]
        [SwaggerResponse(409, "Đang có submission sử dụng phase này")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _phaseService.DeletePhase(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting phase {PhaseId}", id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Danh sách giai đoạn (paging)
        /// </summary>
        [Authorize]
        [HttpGet("list")]
        [SwaggerOperation(
            Summary = "Danh sách giai đoạn",
            Description = "Lấy danh sách phase có phân trang, lọc theo semesterId")]
        [SwaggerResponse(200, "Lấy danh sách giai đoạn thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Produces("application/json")]
        public async Task<IActionResult> List([FromQuery] GetPhasesQueryDTO query)
        {
            try
            {
                var result = await _phaseService.GetPhases(query);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while listing phases");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Chi tiết giai đoạn
        /// </summary>
        [Authorize]
        [HttpGet("detail/{id}")]
        [SwaggerOperation(
            Summary = "Chi tiết giai đoạn",
            Description = "Lấy chi tiết một phase (bao gồm tên semester và phase type)")]
        [SwaggerResponse(200, "Lấy chi tiết giai đoạn thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(404, "Giai đoạn không tồn tại")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Produces("application/json")]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var result = await _phaseService.GetPhaseDetail(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting phase detail {PhaseId}", id);
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}
