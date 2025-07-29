using App.BLL.Interfaces;
using App.Commons;
using App.Commons.BaseAPI;
using App.Entities.Constants;
using App.Entities.DTOs.Topics;
using App.Entities.DTOs.TopicVersions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CapBot.api.Controllers
{
    [Route("api/topic-version")]
    [ApiController]
    public class TopicVersionController : BaseAPIController
    {
        private readonly ITopicVersionService _topicVersionService;
        private readonly ILogger<TopicVersionController> _logger;

        public TopicVersionController(ITopicVersionService topicVersionService, ILogger<TopicVersionController> logger)
        {
            _topicVersionService = topicVersionService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo phiên bản mới cho chủ đề
        /// </summary>
        /// <param name="createTopicVersionDTO">Thông tin tạo phiên bản chủ đề</param>
        /// <returns>Kết quả tạo phiên bản chủ đề</returns>
        /// <remarks>
        /// Tạo phiên bản mới cho chủ đề với trạng thái Draft
        /// Chỉ supervisor của chủ đề mới có thể tạo phiên bản mới
        ///
        /// Sample request:
        ///
        ///     POST /api/topic-version/create
        ///     {
        ///         "topicId": 1,
        ///         "title": "Phát triển ứng dụng di động v2",
        ///         "description": "Mô tả phiên bản 2",
        ///         "objectives": "Mục tiêu cập nhật",
        ///         "methodology": "Phương pháp nghiên cứu mới",
        ///         "expectedOutcomes": "Kết quả mong đợi mới",
        ///         "requirements": "Yêu cầu mới",
        ///         "documentUrl": "http://example.com/doc-v2.pdf"
        ///     }
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Supervisor)]
        [HttpPost("create")]
        [SwaggerOperation(
            Summary = "Tạo phiên bản mới cho chủ đề",
            Description = "Tạo phiên bản mới cho chủ đề với trạng thái Draft"
        )]
        [SwaggerResponse(201, "Tạo phiên bản chủ đề thành công")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Chủ đề không tồn tại")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Create([FromBody] CreateTopicVersionDTO createTopicVersionDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            if (!createTopicVersionDTO.Validate().IsSuccess)
            {
                return ProcessServiceResponse(createTopicVersionDTO.Validate());
            }

            try
            {
                var result = await _topicVersionService.CreateTopicVersion(createTopicVersionDTO, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating topic version");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Cập nhật phiên bản chủ đề
        /// </summary>
        /// <param name="updateTopicVersionDTO">Thông tin cập nhật phiên bản chủ đề</param>
        /// <returns>Kết quả cập nhật phiên bản chủ đề</returns>
        /// <remarks>
        /// Cập nhật phiên bản chủ đề
        /// Chỉ có thể cập nhật phiên bản ở trạng thái Draft
        ///
        /// Sample request:
        ///
        ///     PUT /api/topic-version/update
        ///     {
        ///         "id": 1,
        ///         "title": "Tiêu đề cập nhật",
        ///         "description": "Mô tả cập nhật",
        ///         "objectives": "Mục tiêu cập nhật",
        ///         "methodology": "Phương pháp nghiên cứu cập nhật",
        ///         "expectedOutcomes": "Kết quả mong đợi cập nhật",
        ///         "requirements": "Yêu cầu cập nhật",
        ///         "documentUrl": "http://example.com/doc-updated.pdf"
        ///     }
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Supervisor)]
        [HttpPut("update")]
        [SwaggerOperation(
            Summary = "Cập nhật phiên bản chủ đề",
            Description = "Cập nhật phiên bản chủ đề ở trạng thái Draft"
        )]
        [SwaggerResponse(200, "Cập nhật phiên bản chủ đề thành công")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ hoặc phiên bản không ở trạng thái Draft")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Không tìm thấy phiên bản chủ đề")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Update([FromBody] UpdateTopicVersionDTO updateTopicVersionDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            if (!updateTopicVersionDTO.Validate().IsSuccess)
            {
                return ProcessServiceResponse(updateTopicVersionDTO.Validate());
            }

            try
            {
                var result = await _topicVersionService.UpdateTopicVersion(updateTopicVersionDTO, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating topic version");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy lịch sử các phiên bản của chủ đề
        /// </summary>
        /// <param name="query">Thông tin lọc và phân trang</param>
        /// <param name="topicId">Id của chủ đề</param>
        /// <returns>Danh sách các phiên bản của chủ đề</returns>
        /// <remarks>
        /// Lấy lịch sử tất cả các phiên bản của chủ đề, sắp xếp theo version number giảm dần
        ///
        /// Sample request:
        ///
        ///     GET /api/topic-version/history/1
        ///
        /// </remarks>
        [Authorize]
        [HttpGet("history/{topicId}")]
        [SwaggerOperation(
            Summary = "Lấy lịch sử các phiên bản của chủ đề",
            Description = "Lấy lịch sử tất cả các phiên bản của chủ đề"
        )]
        [SwaggerResponse(200, "Lấy lịch sử phiên bản thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(404, "Không tìm thấy chủ đề")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetHistory([FromQuery] GetTopicVersionQueryDTO query, int topicId)
        {
            try
            {
                var result = await _topicVersionService.GetTopicVersionHistory(query, topicId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting topic version history");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy chi tiết phiên bản chủ đề
        /// </summary>
        /// <param name="versionId">Id của phiên bản</param>
        /// <returns>Chi tiết phiên bản chủ đề</returns>
        /// <remarks>
        /// Lấy chi tiết phiên bản chủ đề bao gồm tất cả thông tin
        ///
        /// Sample request:
        ///
        ///     GET /api/topic-version/detail/1
        ///
        /// </remarks>
        [Authorize]
        [HttpGet("detail/{versionId}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết phiên bản chủ đề",
            Description = "Lấy chi tiết phiên bản chủ đề bao gồm tất cả thông tin"
        )]
        [SwaggerResponse(200, "Lấy chi tiết phiên bản thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(404, "Không tìm thấy phiên bản")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetDetail(int versionId)
        {
            try
            {
                var result = await _topicVersionService.GetTopicVersionDetail(versionId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting topic version detail");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Submit phiên bản chủ đề để review
        /// </summary>
        /// <param name="submitTopicVersionDTO">Thông tin submit phiên bản</param>
        /// <returns>Kết quả submit phiên bản</returns>
        /// <remarks>
        /// Submit phiên bản chủ đề từ trạng thái Draft sang Submitted để review
        /// Chỉ supervisor của chủ đề mới có thể submit
        ///
        /// Sample request:
        ///
        ///     POST /api/topic-version/submit
        ///     {
        ///         "versionId": 1
        ///     }
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Supervisor)]
        [HttpPost("submit")]
        [SwaggerOperation(
            Summary = "Submit phiên bản chủ đề để review",
            Description = "Submit phiên bản chủ đề từ trạng thái Draft sang Submitted"
        )]
        [SwaggerResponse(200, "Submit phiên bản thành công")]
        [SwaggerResponse(400, "Phiên bản không ở trạng thái Draft")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Không tìm thấy phiên bản")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Submit([FromBody] SubmitTopicVersionDTO submitTopicVersionDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            if (!submitTopicVersionDTO.Validate().IsSuccess)
            {
                return ProcessServiceResponse(submitTopicVersionDTO.Validate());
            }

            try
            {
                var result = await _topicVersionService.SubmitTopicVersion(submitTopicVersionDTO, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while submitting topic version");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Review phiên bản chủ đề
        /// </summary>
        /// <param name="reviewTopicVersionDTO">Thông tin review phiên bản</param>
        /// <returns>Kết quả review phiên bản</returns>
        /// <remarks>
        /// Review phiên bản chủ đề và cập nhật trạng thái
        /// Chỉ admin/reviewer mới có thể review
        /// Trạng thái có thể là: Approved, Rejected, RevisionRequired
        ///
        /// Sample request:
        ///
        ///     POST /api/topic-version/review
        ///     {
        ///         "versionId": 1,
        ///         "status": 4,
        ///         "reviewNote": "Phiên bản này cần bổ sung thêm phương pháp nghiên cứu"
        ///     }
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Administrator + "," + SystemRoleConstants.Reviewer)]
        [HttpPost("review")]
        [SwaggerOperation(
            Summary = "Review phiên bản chủ đề",
            Description = "Review phiên bản chủ đề và cập nhật trạng thái"
        )]
        [SwaggerResponse(200, "Review phiên bản thành công")]
        [SwaggerResponse(400, "Phiên bản không ở trạng thái Submitted hoặc status không hợp lệ")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Không tìm thấy phiên bản")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Review([FromBody] ReviewTopicVersionDTO reviewTopicVersionDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            if (!reviewTopicVersionDTO.Validate().IsSuccess)
            {
                return ProcessServiceResponse(reviewTopicVersionDTO.Validate());
            }

            try
            {
                var result = await _topicVersionService.ReviewTopicVersion(reviewTopicVersionDTO, UserId, IsReviewer);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while reviewing topic version");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Xóa phiên bản chủ đề
        /// </summary>
        /// <param name="versionId">Id của phiên bản</param>
        /// <returns>Kết quả xóa phiên bản</returns>
        /// <remarks>
        /// Xóa phiên bản chủ đề (soft delete)
        /// Chỉ có thể xóa phiên bản ở trạng thái Draft
        ///
        /// Sample request:
        ///
        ///     DELETE /api/topic-version/delete/1
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator)]
        [HttpDelete("delete/{versionId}")]
        [SwaggerOperation(
            Summary = "Xóa phiên bản chủ đề",
            Description = "Xóa phiên bản chủ đề (soft delete)"
        )]
        [SwaggerResponse(200, "Xóa phiên bản thành công")]
        [SwaggerResponse(400, "Phiên bản không ở trạng thái Draft")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Không tìm thấy phiên bản")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Delete(int versionId)
        {
            try
            {
                var result = await _topicVersionService.DeleteTopicVersion(versionId, UserId, IsAdmin);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting topic version");
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}
