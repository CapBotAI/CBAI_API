using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using App.BLL.Interfaces;
using App.Entities.DTOs.Review;
using App.Commons.BaseAPI;
using App.Commons;
using App.Commons.Paging;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace CapBot.api.Controllers
{
    [Route("api/reviews")]
    [ApiController]
    [Authorize]
    public class ReviewController : BaseAPIController
    {
        private readonly IReviewService _reviewService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            IReviewService reviewService,
            ILogger<ReviewController> logger)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo mới đánh giá
        /// </summary>
        /// <param name="createDTO">Thông tin đánh giá</param>
        /// <returns>Đánh giá được tạo</returns>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo mới đánh giá",
            Description = "Tạo đánh giá mới với điểm số cho từng tiêu chí"
        )]
        [SwaggerResponse(201, "Tạo đánh giá thành công")]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy assignment")]
        [SwaggerResponse(409, "Assignment đã có đánh giá")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> Create([FromBody] CreateReviewDTO createDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _reviewService.CreateAsync(createDTO, currentUserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating review for assignment {AssignmentId}", createDTO.AssignmentId);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Cập nhật đánh giá
        /// </summary>
        /// <param name="updateDTO">Thông tin cập nhật đánh giá</param>
        /// <returns>Đánh giá được cập nhật</returns>
        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật đánh giá",
            Description = "Cập nhật thông tin đánh giá và điểm số"
        )]
        [SwaggerResponse(200, "Cập nhật đánh giá thành công")]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ hoặc đánh giá đã submit")]
        [SwaggerResponse(404, "Không tìm thấy đánh giá")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> Update([FromBody] UpdateReviewDTO updateDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            try
            {
                var result = await _reviewService.UpdateAsync(updateDTO);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating review with ID {Id}", updateDTO.Id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Xóa đánh giá
        /// </summary>
        /// <param name="id">ID của đánh giá cần xóa</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa đánh giá",
            Description = "Xóa đánh giá theo ID (chỉ xóa được draft)"
        )]
        [SwaggerResponse(200, "Xóa đánh giá thành công")]
        [SwaggerResponse(400, "Không thể xóa đánh giá đã submit")]
        [SwaggerResponse(404, "Không tìm thấy đánh giá")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _reviewService.DeleteAsync(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting review with ID {Id}", id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy thông tin đánh giá theo ID
        /// </summary>
        /// <param name="id">ID của đánh giá</param>
        /// <returns>Thông tin đánh giá chi tiết</returns>
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin đánh giá theo ID",
            Description = "Lấy thông tin chi tiết của đánh giá bao gồm điểm số từng tiêu chí"
        )]
        [SwaggerResponse(200, "Lấy thông tin thành công")]
        [SwaggerResponse(404, "Không tìm thấy đánh giá")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _reviewService.GetByIdAsync(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting review with ID {Id}", id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy danh sách đánh giá có phân trang
        /// </summary>
        /// <param name="pagingModel">Thông tin phân trang</param>
        /// <returns>Danh sách đánh giá có phân trang</returns>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách đánh giá có phân trang",
            Description = "Lấy danh sách đánh giá với phân trang"
        )]
        [SwaggerResponse(200, "Lấy danh sách thành công")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetAll([FromQuery] PagingModel pagingModel)
        {
            try
            {
                var result = await _reviewService.GetAllAsync(pagingModel);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting reviews list");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Submit đánh giá
        /// </summary>
        /// <param name="id">ID của đánh giá cần submit</param>
        /// <returns>Đánh giá đã được submit</returns>
        [HttpPost("{id}/submit")]
        [SwaggerOperation(
            Summary = "Submit đánh giá",
            Description = "Submit đánh giá để hoàn thành quá trình đánh giá"
        )]
        [SwaggerResponse(200, "Submit đánh giá thành công")]
        [SwaggerResponse(400, "Đánh giá đã được submit")]
        [SwaggerResponse(404, "Không tìm thấy đánh giá")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> SubmitReview(int id)
        {
            try
            {
                var result = await _reviewService.SubmitReviewAsync(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while submitting review with ID {Id}", id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy danh sách đánh giá theo assignment
        /// </summary>
        /// <param name="assignmentId">ID của assignment</param>
        /// <returns>Danh sách đánh giá của assignment</returns>
        [HttpGet("assignment/{assignmentId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách đánh giá theo assignment",
            Description = "Lấy tất cả đánh giá của một assignment"
        )]
        [SwaggerResponse(200, "Lấy danh sách thành công")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetReviewsByAssignment(int assignmentId)
        {
            try
            {
                var result = await _reviewService.GetReviewsByAssignmentAsync(assignmentId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting reviews for assignment {AssignmentId}", assignmentId);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy thống kê đánh giá theo trạng thái
        /// </summary>
        /// <returns>Thống kê số lượng đánh giá theo trạng thái</returns>
        [HttpGet("statistics")]
        [SwaggerOperation(
            Summary = "Lấy thống kê đánh giá",
            Description = "Lấy thống kê số lượng đánh giá theo trạng thái (Draft, Submitted)"
        )]
        [SwaggerResponse(200, "Lấy thống kê thành công")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                // Có thể implement thêm method GetStatisticsAsync trong service
                var result = await _reviewService.GetAllAsync(new PagingModel { PageNumber = 1, PageSize = int.MaxValue });
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting review statistics");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy bảng điểm chi tiết của một đánh giá
        /// </summary>
        /// <param name="id">ID của đánh giá</param>
        /// <returns>Bảng điểm chi tiết</returns>
        [HttpGet("{id}/scores")]
        [SwaggerOperation(
            Summary = "Lấy bảng điểm chi tiết",
            Description = "Lấy bảng điểm chi tiết với điểm từng tiêu chí của một đánh giá"
        )]
        [SwaggerResponse(200, "Lấy bảng điểm thành công")]
        [SwaggerResponse(404, "Không tìm thấy đánh giá")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetScoreBoard(int id)
        {
            try
            {
                var result = await _reviewService.GetByIdAsync(id);
                if (result.IsSuccess && result.Data != null)
                {
                    // Trả về chỉ phần điểm số
                    var scoreData = new
                    {
                        ReviewId = result.Data.Id,
                        OverallScore = result.Data.OverallScore,
                        CriteriaScores = result.Data.CriteriaScores.Select(cs => new
                        {
                            CriteriaId = cs.CriteriaId,
                            CriteriaName = cs.Criteria.Name,
                            Score = cs.Score,
                            MaxScore = cs.Criteria.MaxScore,
                            Weight = cs.Criteria.Weight,
                            Comment = cs.Comment
                        }).ToList()
                    };
                    
                    return Ok(new { IsSuccess = true, Data = scoreData });
                }
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting score board for review {Id}", id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Rút lại đánh giá đã submit (chuyển về Draft)
        /// </summary>
        /// <param name="id">ID của đánh giá cần rút lại</param>
        /// <returns>Kết quả rút lại đánh giá</returns>
        [HttpPost("{id}/withdraw")]
        [SwaggerOperation(
            Summary = "Rút lại đánh giá",
            Description = "Rút lại đánh giá đã submit, chuyển về trạng thái Draft để có thể chỉnh sửa"
        )]
        [SwaggerResponse(200, "Rút lại đánh giá thành công")]
        [SwaggerResponse(400, "Đánh giá chưa được submit")]
        [SwaggerResponse(404, "Không tìm thấy đánh giá")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> WithdrawReview(int id)
        {
            try
            {
                var result = await _reviewService.WithdrawReviewAsync(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while withdrawing review with ID {Id}", id);
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}