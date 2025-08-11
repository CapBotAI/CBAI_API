using App.BLL.Interfaces;
using App.Commons;
using App.Commons.BaseAPI;
using App.Entities.Constants;
using App.Entities.DTOs.ReviewerSuggestion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        /// Gợi ý reviewer phù hợp cho phiên bản chủ đề
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
        [HttpPost("ai-suggest")]
        [SwaggerOperation(
            Summary = "AI agent gợi ý reviewer phù hợp cho phiên bản chủ đề",
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
        /// Lấy danh sách reviewer ít workload nhất cho một phiên bản chủ đề
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
        [HttpGet("top")]
        [SwaggerOperation(
            Summary = "Lấy danh sách reviewer ít workload nhất cho một phiên bản chủ đề",
            Description = "Dựa trên số lượng assignment đang hoạt động, kỹ năng và hiệu suất"
        )]
        [SwaggerResponse(200, "Danh sách reviewer thành công")]
        [Produces("application/json")]
        public async Task<IActionResult> GetTopReviewers([FromQuery] int topicVersionId, [FromQuery] int count = 5)
        {
            try
            {
                var input = new ReviewerSuggestionInputDTO
                {
                    TopicVersionId = topicVersionId,
                    MaxSuggestions = count,
                    UsePrompt = false
                };
                var result = await _reviewerSuggestionService.SuggestReviewersAsync(input);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting top reviewers");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Gợi ý reviewer cho nhiều phiên bản chủ đề (bulk)
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
        [HttpPost("bulk-ai-suggest")]
        [SwaggerOperation(
            Summary = "Gợi ý reviewer cho nhiều phiên bản chủ đề",
            Description = "Gợi ý reviewer sử dụng AI cho nhiều TopicVersionId cùng lúc"
        )]
        [SwaggerResponse(200, "Bulk reviewer suggestions successful")]
        [SwaggerResponse(400, "Invalid input")]
        [SwaggerResponse(500, "Internal server error")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> BulkSuggest([FromBody] BulkReviewerSuggestionInputDTO input)
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            if (input == null || input.TopicVersionIds == null || !input.TopicVersionIds.Any())
                return Error("Dữ liệu đầu vào không hợp lệ");

            try
            {
                var result = await _reviewerSuggestionService.BulkSuggestReviewersAsync(input);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while bulk suggesting reviewers");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Kiểm tra reviewer có đủ điều kiện cho một phiên bản chủ đề không
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
        [HttpGet("check-eligibility")]
        [SwaggerOperation(
            Summary = "Kiểm tra reviewer có đủ điều kiện cho một phiên bản chủ đề",
            Description = "Kiểm tra eligibility dựa trên kỹ năng, workload, hiệu suất, v.v."
        )]
        [SwaggerResponse(200, "Kiểm tra eligibility thành công")]
        [SwaggerResponse(404, "Reviewer không tìm thấy")]
        [Produces("application/json")]
        public async Task<IActionResult> CheckEligibility([FromQuery] int reviewerId, [FromQuery] int topicVersionId)
        {
            try
            {
                var result = await _reviewerSuggestionService.CheckReviewerEligibilityAsync(reviewerId, topicVersionId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking reviewer eligibility");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy lịch sử gợi ý reviewer cho một phiên bản chủ đề
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
        [HttpGet("history")]
        [SwaggerOperation(
            Summary = "Lấy lịch sử gợi ý reviewer cho một phiên bản chủ đề",
            Description = "Trả về lịch sử các lần gợi ý reviewer cho topic version"
        )]
        [SwaggerResponse(200, "Lấy lịch sử thành công")]
        [Produces("application/json")]
        public async Task<IActionResult> SuggestionHistory([FromQuery] int topicVersionId)
        {
            try
            {
                var result = await _reviewerSuggestionService.GetSuggestionHistoryAsync(topicVersionId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suggestion history");
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}