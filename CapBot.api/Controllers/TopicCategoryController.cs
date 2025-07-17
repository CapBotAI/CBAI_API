using App.BLL.Interfaces;
using App.Commons;
using App.Commons.BaseAPI;
using App.Entities.Constants;
using App.Entities.DTOs.TopicCategories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CapBot.api.Controllers
{
    [Route("api/topic-category")]
    [ApiController]
    public class TopicCategoryController : BaseAPIController
    {
        private readonly ITopicCategoryService _topicCategoryService;
        private readonly ILogger<TopicCategoryController> _logger;

        public TopicCategoryController(ITopicCategoryService topicCategoryService, ILogger<TopicCategoryController> logger)
        {
            _topicCategoryService = topicCategoryService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo danh mục chủ đề mới
        /// </summary>
        /// <param name="createTopicCategoryDTO">Thông tin tạo danh mục chủ đề</param>
        /// <returns>Kết quả tạo danh mục chủ đề</returns>
        /// <remarks>
        /// Tạo danh mục chủ đề mới với thông tin đầy đủ
        /// - Tên danh mục (bắt buộc)
        /// - Mô tả (tùy chọn)
        ///
        /// Sample request:
        ///
        ///     POST /api/topic-category/create
        ///     {
        ///         "name": "Công nghệ thông tin",
        ///         "description": "Các chủ đề liên quan đến công nghệ thông tin"
        ///     }
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpPost("create")]
        [SwaggerOperation(
            Summary = "Tạo danh mục chủ đề mới",
            Description = "Tạo danh mục chủ đề mới với thông tin đầy đủ"
        )]
        [SwaggerResponse(201, "Tạo danh mục chủ đề thành công")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(409, "Tên danh mục chủ đề đã tồn tại")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Create([FromBody] CreateTopicCategoryDTO createTopicCategoryDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            if (!createTopicCategoryDTO.Validate().IsSuccess)
            {
                return ProcessServiceResponse(createTopicCategoryDTO.Validate());
            }

            try
            {
                var result = await _topicCategoryService.CreateTopicCategory(createTopicCategoryDTO, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating topic category");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy danh sách danh mục chủ đề
        /// </summary>
        /// <returns>Danh sách danh mục chủ đề</returns>
        /// <remarks>
        /// Lấy danh sách tất cả danh mục chủ đề
        ///
        /// Sample request:
        ///
        ///     GET /api/topic-category/all
        ///
        /// </remarks>
        [Authorize]
        [HttpGet("all")]
        [SwaggerOperation(
            Summary = "Lấy danh sách danh mục chủ đề",
            Description = "Lấy danh sách tất cả danh mục chủ đề"
        )]
        [SwaggerResponse(200, "Lấy danh sách danh mục chủ đề thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _topicCategoryService.GetAllTopicCategory();
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all topic categories");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Cập nhật danh mục chủ đề
        /// </summary>
        /// <param name="updateTopicCategoryDTO">Thông tin cập nhật danh mục chủ đề</param>
        /// <returns>Kết quả cập nhật danh mục chủ đề</returns>
        /// <remarks>
        /// Cập nhật danh mục chủ đề với thông tin đầy đủ
        /// - Id danh mục (bắt buộc)
        /// - Tên danh mục (bắt buộc)
        /// - Mô tả (tùy chọn)
        ///
        /// Sample request:
        ///
        ///     PUT /api/topic-category/update
        ///     {
        ///         "id": 1,
        ///         "name": "Công nghệ thông tin cập nhật",
        ///         "description": "Mô tả mới cho danh mục"
        ///     }
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpPut("update")]
        [SwaggerOperation(
            Summary = "Cập nhật danh mục chủ đề",
            Description = "Cập nhật danh mục chủ đề với thông tin đầy đủ"
        )]
        [SwaggerResponse(200, "Cập nhật danh mục chủ đề thành công")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Không tìm thấy danh mục chủ đề")]
        [SwaggerResponse(409, "Tên danh mục chủ đề đã tồn tại")]
        [SwaggerResponse(422, "Model không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Update([FromBody] UpdateTopicCategoryDTO updateTopicCategoryDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            if (!updateTopicCategoryDTO.Validate().IsSuccess)
            {
                return ProcessServiceResponse(updateTopicCategoryDTO.Validate());
            }

            try
            {
                var result = await _topicCategoryService.UpdateTopicCategory(updateTopicCategoryDTO, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating topic category");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy chi tiết danh mục chủ đề
        /// </summary>
        /// <param name="topicCategoryId">Id của danh mục chủ đề</param>
        /// <returns>Chi tiết danh mục chủ đề</returns>
        /// <remarks>
        /// Lấy chi tiết danh mục chủ đề bao gồm số lượng chủ đề
        ///
        /// Sample request:
        ///
        ///     GET /api/topic-category/detail/1
        ///
        /// </remarks>
        [Authorize]
        [HttpGet("detail/{topicCategoryId}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết danh mục chủ đề",
            Description = "Lấy chi tiết danh mục chủ đề bao gồm số lượng chủ đề"
        )]
        [SwaggerResponse(200, "Lấy chi tiết danh mục chủ đề thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(404, "Không tìm thấy danh mục chủ đề")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetDetail(int topicCategoryId)
        {
            try
            {
                var result = await _topicCategoryService.GetTopicCategoryDetail(topicCategoryId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting topic category detail");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Xóa danh mục chủ đề
        /// </summary>
        /// <param name="topicCategoryId">Id của danh mục chủ đề</param>
        /// <returns>Kết quả xóa danh mục chủ đề</returns>
        /// <remarks>
        /// Xóa danh mục chủ đề (soft delete)
        /// Lưu ý: Không thể xóa danh mục đang có chủ đề sử dụng
        ///
        /// Sample request:
        ///
        ///     DELETE /api/topic-category/delete/1
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpDelete("delete/{topicCategoryId}")]
        [SwaggerOperation(
            Summary = "Xóa danh mục chủ đề",
            Description = "Xóa danh mục chủ đề (soft delete)"
        )]
        [SwaggerResponse(200, "Xóa danh mục chủ đề thành công")]
        [SwaggerResponse(400, "Không thể xóa danh mục đang có chủ đề sử dụng")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(404, "Không tìm thấy danh mục chủ đề")]
        [SwaggerResponse(409, "Không thể xóa danh mục đang có chủ đề sử dụng")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Delete(int topicCategoryId)
        {
            try
            {
                var result = await _topicCategoryService.DeleteTopicCategory(topicCategoryId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting topic category");
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}
