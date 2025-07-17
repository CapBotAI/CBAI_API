using App.BLL.Interfaces;
using App.Commons;
using App.Commons.BaseAPI;
using App.Commons.Paging;
using App.Entities.Constants;
using App.Entities.DTOs.Topics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CapBot.api.Controllers
{
    [Route("api/topic")]
    [ApiController]
    public class TopicController : BaseAPIController
    {
        private readonly ITopicService _topicService;
        private readonly ILogger<TopicController> _logger;

        public TopicController(ITopicService topicService, ILogger<TopicController> logger)
        {
            _topicService = topicService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo chủ đề mới
        /// </summary>
        /// <param name="createTopicDTO">Thông tin tạo chủ đề</param>
        /// <returns>Kết quả tạo chủ đề</returns>
        /// <remarks>
        /// Tạo chủ đề mới với phiên bản đầu tiên
        /// - Tiêu đề (bắt buộc)
        /// - Mô tả, mục tiêu (tùy chọn)
        /// - Danh mục và học kỳ (bắt buộc)
        /// - Số sinh viên tối đa (1-10)
        /// - Thông tin chi tiết cho phiên bản đầu tiên
        ///
        /// Sample request:
        ///
        ///     POST /api/topic/create
        ///     {
        ///         "title": "Phát triển ứng dụng di động",
        ///         "description": "Mô tả chủ đề",
        ///         "objectives": "Mục tiêu nghiên cứu",
        ///         "categoryId": 1,
        ///         "semesterId": 1,
        ///         "maxStudents": 2,
        ///         "methodology": "Phương pháp nghiên cứu",
        ///         "expectedOutcomes": "Kết quả mong đợi",
        ///         "requirements": "Yêu cầu",
        ///         "documentUrl": "http://example.com/doc.pdf"
        ///     }
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Supervisor)]
        [HttpPost("create")]
        [SwaggerOperation(
            Summary = "Tạo chủ đề mới",
            Description = "Tạo chủ đề mới với phiên bản đầu tiên"
        )]
        [SwaggerResponse(201, "Tạo chủ đề thành công")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(409, "Danh mục hoặc học kỳ không tồn tại")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Create([FromBody] CreateTopicDTO createTopicDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            if (!createTopicDTO.Validate().IsSuccess)
            {
                return ProcessServiceResponse(createTopicDTO.Validate());
            }

            try
            {
                var result = await _topicService.CreateTopic(createTopicDTO, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating topic");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy danh sách chủ đề
        /// </summary>
        /// <param name="query">Thông tin lọc</param>
        /// <returns>Danh sách chủ đề</returns>
        /// <remarks>
        /// Lấy danh sách chủ đề với bộ lọc tùy chọn
        ///
        /// Sample request:
        ///
        ///     GET /api/topic/list
        ///     GET /api/topic/list?semesterId=1
        ///     GET /api/topic/list?categoryId=1
        ///     GET /api/topic/list?semesterId=1&categoryId=1
        ///
        /// </remarks>
        [Authorize]
        [HttpGet("list")]
        [SwaggerOperation(
            Summary = "Lấy danh sách chủ đề",
            Description = "Lấy danh sách chủ đề với bộ lọc tùy chọn"
        )]
        [SwaggerResponse(200, "Lấy danh sách chủ đề thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetAll([FromQuery] GetTopicsQueryDTO query)
        {
            try
            {
                var result = await _topicService.GetAllTopics(query);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all topics");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy chi tiết chủ đề
        /// </summary>
        /// <param name="topicId">Id của chủ đề</param>
        /// <returns>Chi tiết chủ đề</returns>
        /// <remarks>
        /// Lấy chi tiết chủ đề bao gồm phiên bản hiện tại
        ///
        /// Sample request:
        ///
        ///     GET /api/topic/detail/1
        ///
        /// </remarks>
        [Authorize]
        [HttpGet("detail/{topicId}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết chủ đề",
            Description = "Lấy chi tiết chủ đề bao gồm phiên bản hiện tại"
        )]
        [SwaggerResponse(200, "Lấy chi tiết chủ đề thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(404, "Không tìm thấy chủ đề")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetDetail(int topicId)
        {
            try
            {
                var result = await _topicService.GetTopicDetail(topicId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting topic detail");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Cập nhật chủ đề
        /// </summary>
        /// <param name="updateTopicDTO">Thông tin cập nhật chủ đề</param>
        /// <returns>Kết quả cập nhật chủ đề</returns>
        /// <remarks>
        /// Cập nhật thông tin cơ bản của chủ đề
        /// Chỉ supervisor hoặc admin mới có thể cập nhật
        ///
        /// Sample request:
        ///
        ///     PUT /api/topic/update
        ///     {
        ///         "id": 1,
        ///         "title": "Tiêu đề cập nhật",
        ///         "description": "Mô tả cập nhật",
        ///         "objectives": "Mục tiêu cập nhật",
        ///         "categoryId": 1,
        ///         "maxStudents": 3
        ///     }
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator)]
        [HttpPut("update")]
        [SwaggerOperation(
            Summary = "Cập nhật chủ đề",
            Description = "Cập nhật thông tin cơ bản của chủ đề"
        )]
        [SwaggerResponse(200, "Cập nhật chủ đề thành công")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Không tìm thấy chủ đề")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Update([FromBody] UpdateTopicDTO updateTopicDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            if (!updateTopicDTO.Validate().IsSuccess)
            {
                return ProcessServiceResponse(updateTopicDTO.Validate());
            }

            try
            {
                var result = await _topicService.UpdateTopic(updateTopicDTO, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating topic");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Xóa chủ đề
        /// </summary>
        /// <param name="topicId">Id của chủ đề</param>
        /// <returns>Kết quả xóa chủ đề</returns>
        /// <remarks>
        /// Xóa chủ đề (soft delete)
        ///
        /// Sample request:
        ///
        ///     DELETE /api/topic/delete/1
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpDelete("delete/{topicId}")]
        [SwaggerOperation(
            Summary = "Xóa chủ đề",
            Description = "Xóa chủ đề (soft delete)"
        )]
        [SwaggerResponse(200, "Xóa chủ đề thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Không tìm thấy chủ đề")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Delete(int topicId)
        {
            try
            {
                var result = await _topicService.DeleteTopic(topicId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting topic");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Phê duyệt chủ đề
        /// </summary>
        /// <param name="topicId">Id của chủ đề</param>
        /// <returns>Kết quả phê duyệt chủ đề</returns>
        /// <remarks>
        /// Phê duyệt chủ đề - chỉ admin mới có quyền
        ///
        /// Sample request:
        ///
        ///     POST /api/topic/approve/1
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpPost("approve/{topicId}")]
        [SwaggerOperation(
            Summary = "Phê duyệt chủ đề",
            Description = "Phê duyệt chủ đề - chỉ admin mới có quyền"
        )]
        [SwaggerResponse(200, "Phê duyệt chủ đề thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Không tìm thấy chủ đề")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Approve(int topicId)
        {
            try
            {
                var result = await _topicService.ApproveTopic(topicId, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while approving topic");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy danh sách chủ đề của tôi
        /// </summary>
        /// <returns>Danh sách chủ đề của supervisor hiện tại</returns>
        /// <remarks>
        /// Lấy danh sách chủ đề mà supervisor hiện tại đã tạo
        ///
        /// Sample request:
        ///
        ///     GET /api/topic/my-topics
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Supervisor)]
        [HttpGet("my-topics")]
        [SwaggerOperation(
            Summary = "Lấy danh sách chủ đề của tôi",
            Description = "Lấy danh sách chủ đề của supervisor hiện tại"
        )]
        [SwaggerResponse(200, "Lấy danh sách chủ đề thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetMyTopics()
        {
            try
            {
                var result = await _topicService.GetMyTopics(UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting my topics");
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}
