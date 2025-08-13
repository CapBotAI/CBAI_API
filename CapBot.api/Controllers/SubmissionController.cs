using App.BLL.Interfaces;
using App.Commons;
using App.Commons.BaseAPI;
using App.Entities.Constants;
using App.Entities.DTOs.Submissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CapBot.api.Controllers
{
    [Route("api/submission")]
    [ApiController]
    public class SubmissionController : BaseAPIController
    {
        private readonly ISubmissionService _submissionService;
        private readonly ILogger<SubmissionController> _logger;

        public SubmissionController(ISubmissionService submissionService, ILogger<SubmissionController> logger)
        {
            _submissionService = submissionService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo submission mới
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor)]
        [HttpPost("create")]
        [SwaggerOperation(
        Summary = "Tạo submission mới",
        Description = "Supervisor tạo submission trực tiếp từ một topic version (không cần topic version Approved) thuộc phase hợp lệ")]
        [SwaggerResponse(201, "Tạo submission thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Phiên bản chủ đề hoặc giai đoạn không tồn tại")]
        [SwaggerResponse(409, "Giai đoạn không cùng học kỳ với chủ đề")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Create([FromBody] CreateSubmissionDTO dto) // giữ nguyên phần thân
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            var val = ((App.Commons.Interfaces.IValidationPipeline)dto).Validate();
            if (!val.IsSuccess)
                return ProcessServiceResponse(val);

            try
            {
                var result = await _submissionService.CreateSubmission(dto, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating submission");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Cập nhật submission
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor)]
        [HttpPut("update")]
        [SwaggerOperation(
            Summary = "Cập nhật submission",
            Description = "Cập nhật nội dung submission khi ở trạng thái Pending/RevisionRequired")]
        [SwaggerResponse(200, "Cập nhật submission thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Không có quyền")]
        [SwaggerResponse(404, "Submission/Phase/TopicVersion không tồn tại")]
        [SwaggerResponse(409, "Ràng buộc học kỳ không hợp lệ")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Update([FromBody] UpdateSubmissionDTO dto)
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            var val = ((App.Commons.Interfaces.IValidationPipeline)dto).Validate();
            if (!val.IsSuccess)
                return ProcessServiceResponse(val);

            try
            {
                var result = await _submissionService.UpdateSubmission(dto, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating submission {SubmissionId}", dto.Id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Submit submission (Pending → UnderReview)
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor)]
        [HttpPost("submit")]
        [SwaggerOperation(
            Summary = "Submit submission",
            Description = "Chuyển trạng thái submission từ Pending sang UnderReview trước deadline")]
        [SwaggerResponse(200, "Submit submission thành công")]
        [SwaggerResponse(400, "Trạng thái không hợp lệ hoặc quá hạn")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Không có quyền")]
        [SwaggerResponse(404, "Submission không tồn tại")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Submit([FromBody] SubmitSubmissionDTO dto)
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            var val = ((App.Commons.Interfaces.IValidationPipeline)dto).Validate();
            if (!val.IsSuccess)
                return ProcessServiceResponse(val);

            try
            {
                var result = await _submissionService.SubmitSubmission(dto, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while submitting submission {SubmissionId}", dto.Id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Resubmit submission (RevisionRequired → UnderReview)
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor)]
        [HttpPost("resubmit")]
        [SwaggerOperation(
            Summary = "Resubmit submission",
            Description = "Chuyển trạng thái submission từ RevisionRequired sang UnderReview trước deadline, tăng round")]
        [SwaggerResponse(200, "Resubmit submission thành công")]
        [SwaggerResponse(400, "Trạng thái không hợp lệ hoặc quá hạn")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Không có quyền")]
        [SwaggerResponse(404, "Submission không tồn tại")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Resubmit([FromBody] ResubmitSubmissionDTO dto)
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            var val = ((App.Commons.Interfaces.IValidationPipeline)dto).Validate();
            if (!val.IsSuccess)
                return ProcessServiceResponse(val);

            try
            {
                var result = await _submissionService.ResubmitSubmission(dto, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while resubmitting submission {SubmissionId}", dto.Id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Chi tiết submission
        /// </summary>
        [Authorize]
        [HttpGet("detail/{id}")]
        [SwaggerOperation(
            Summary = "Chi tiết submission",
            Description = "Lấy chi tiết submission kèm thông tin liên quan")]
        [SwaggerResponse(200, "Lấy chi tiết submission thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(404, "Submission không tồn tại")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Produces("application/json")]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var result = await _submissionService.GetSubmissionDetail(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting submission detail {SubmissionId}", id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Danh sách submission (paging/filter)
        /// </summary>
        [Authorize]
        [HttpGet("list")]
        [SwaggerOperation(
            Summary = "Danh sách submission",
            Description = "Lấy danh sách submission có phân trang, lọc theo TopicVersion/Phase/Semester/Status")]
        [SwaggerResponse(200, "Lấy danh sách submission thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Produces("application/json")]
        public async Task<IActionResult> List([FromQuery] GetSubmissionsQueryDTO query)
        {
            try
            {
                var result = await _submissionService.GetSubmissions(query);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while listing submissions");
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}
