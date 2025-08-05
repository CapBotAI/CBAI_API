using App.BLL.Interfaces;
using App.Commons;
using App.Commons.BaseAPI;
using App.Entities.Constants;
using App.Entities.DTOs.ReviewerSuggestion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace CapBot.api.Controllers
{
    [Route("api/reviewer-suggestion")]
    [ApiController]
    public class ReviewerSuggestionController : BaseAPIController
    {
        private readonly IReviewerSuggestionService _reviewerSuggestionService;
        private readonly ILogger<ReviewerSuggestionController> _logger;

        public ReviewerSuggestionController(
            IReviewerSuggestionService reviewerSuggestionService,
            ILogger<ReviewerSuggestionController> logger)
        {
            _reviewerSuggestionService = reviewerSuggestionService;
            _logger = logger;
        }

        /// <summary>
        /// Gợi ý reviewer phù hợp cho phiên bản chủ đề (Supervisor/Admin/Moderator)
        /// </summary>
        /// <param name="input">Thông tin đầu vào</param>
        /// <returns>Danh sách reviewer phù hợp</returns>
        /// <remarks>
        /// - Chỉ Supervisor, Admin hoặc Moderator mới có thể gọi API này.
        /// - Gợi ý reviewer dựa trên kỹ năng, workload, hiệu suất, v.v.
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
        [HttpPost("suggest")]
        [SwaggerOperation(
            Summary = "Gợi ý reviewer phù hợp cho phiên bản chủ đề",
            Description = "Chỉ Supervisor/Admin/Moderator có quyền truy cập"
        )]
        [SwaggerResponse(200, "Gợi ý reviewer thành công")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Suggest([FromBody] ReviewerSuggestionInputDTO input)
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            if (input == null)
                return Error("Dữ liệu đầu vào không hợp lệ");

            try
            {
                // You may add audit log, user info, or extra logic here
                var result = await _reviewerSuggestionService.SuggestReviewersAsync(input);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while suggesting reviewers");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Kiểm tra nhanh khả năng phù hợp của một reviewer với một topic version
        /// </summary>
        /// <param name="topicVersionId">Id phiên bản chủ đề</param>
        /// <param name="reviewerId">Id reviewer</param>
        /// <returns>Điểm số và đánh giá chi tiết</returns>
        /// <remarks>
        /// - Dùng cho giao diện chi tiết reviewer hoặc khi muốn kiểm tra nhanh 1 reviewer với 1 topic.
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
        [HttpGet("quick-score")]
        [SwaggerOperation(
            Summary = "Kiểm tra nhanh khả năng phù hợp của reviewer với topic version",
            Description = "Trả về điểm số và đánh giá chi tiết"
        )]
        [SwaggerResponse(200, "Tính điểm thành công")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Produces("application/json")]
        public async Task<IActionResult> QuickScore([FromQuery] int topicVersionId, [FromQuery] int reviewerId)
        {
            if (topicVersionId <= 0 || reviewerId <= 0)
                return Error("Thiếu thông tin hoặc thông tin không hợp lệ");

            try
            {
                // This method should be implemented in your service (add if missing)
                var scoreResult = await _reviewerSuggestionService.QuickScoreAsync(topicVersionId, reviewerId);
                return ProcessServiceResponse(scoreResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while quick scoring reviewer with topic version");
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}