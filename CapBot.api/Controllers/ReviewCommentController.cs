using Microsoft.AspNetCore.Mvc;
using App.BLL.Interfaces;
using App.Entities.DTOs.ReviewComment;
using App.Commons.BaseAPI;
using App.Commons;
using App.Commons.Paging;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace CapBot.api.Controllers
{
    [Route("api/review-comments")]
    [ApiController]
    [Authorize]
    public class ReviewCommentController : BaseAPIController
    {
        private readonly IReviewCommentService _reviewCommentService;
        private readonly ILogger<ReviewCommentController> _logger;

        public ReviewCommentController(
            IReviewCommentService reviewCommentService,
            ILogger<ReviewCommentController> logger)
        {
            _reviewCommentService = reviewCommentService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo comment mới cho review
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Tạo comment mới cho review")]
        [SwaggerResponse(201, "Tạo comment thành công")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Review không tồn tại")]
        public async Task<IActionResult> CreateComment([FromBody] CreateReviewCommentDTO createDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _reviewCommentService.CreateAsync(createDTO);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review comment");
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Cập nhật comment
        /// </summary>
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Cập nhật comment")]
        [SwaggerResponse(200, "Cập nhật thành công")]
        [SwaggerResponse(404, "Comment không tồn tại")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateReviewCommentDTO updateDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _reviewCommentService.UpdateAsync(id, updateDTO);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {Id}", id);
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Xóa comment
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Xóa comment")]
        [SwaggerResponse(200, "Xóa thành công")]
        [SwaggerResponse(404, "Comment không tồn tại")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var result = await _reviewCommentService.DeleteAsync(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {Id}", id);
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Lấy comment theo ID
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Lấy comment theo ID")]
        [SwaggerResponse(200, "Lấy thông tin thành công")]
        [SwaggerResponse(404, "Comment không tồn tại")]
        public async Task<IActionResult> GetCommentById(int id)
        {
            try
            {
                var result = await _reviewCommentService.GetByIdAsync(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment {Id}", id);
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Lấy comment theo Review ID
        /// </summary>
        [HttpGet("by-review/{reviewId}")]
        [SwaggerOperation(Summary = "Lấy comment theo Review ID")]
        [SwaggerResponse(200, "Lấy danh sách thành công")]
        public async Task<IActionResult> GetCommentsByReviewId(
            int reviewId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var pagingModel = new PagingModel { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _reviewCommentService.GetByReviewIdAsync(reviewId, pagingModel);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for review {ReviewId}", reviewId);
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Lấy tất cả comment
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Lấy tất cả comment với phân trang")]
        [SwaggerResponse(200, "Lấy danh sách thành công")]
        public async Task<IActionResult> GetAllComments(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var pagingModel = new PagingModel { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _reviewCommentService.GetAllAsync(pagingModel);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all comments");
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Đánh dấu comment đã giải quyết
        /// </summary>
        [HttpPatch("{id}/resolve")]
        [SwaggerOperation(Summary = "Đánh dấu comment đã giải quyết")]
        [SwaggerResponse(200, "Đánh dấu thành công")]
        [SwaggerResponse(404, "Comment không tồn tại")]
        public async Task<IActionResult> MarkAsResolved(int id)
        {
            try
            {
                var result = await _reviewCommentService.MarkAsResolvedAsync(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking comment {Id} as resolved", id);
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Lấy comment chưa giải quyết
        /// </summary>
        [HttpGet("unresolved")]
        [SwaggerOperation(Summary = "Lấy comment chưa giải quyết")]
        [SwaggerResponse(200, "Lấy danh sách thành công")]
        public async Task<IActionResult> GetUnresolvedComments(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var pagingModel = new PagingModel { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _reviewCommentService.GetUnresolvedCommentsAsync(pagingModel);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unresolved comments");
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }
    }
}