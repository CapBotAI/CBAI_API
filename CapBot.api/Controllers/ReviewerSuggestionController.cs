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
        public async Task<IActionResult> Suggest([FromBody] App.Entities.DTOs.ReviewerSuggestion.ReviewerSuggestionInputDTO input)
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            if (input == null)
                return Error("Invalid input data");

            try
            {
                _logger.LogInformation("Suggest called with TopicVersionId={TopicVersionId} MaxSuggestions={MaxSuggestions} UsePrompt={UsePrompt}", input.TopicVersionId, input.MaxSuggestions, input.UsePrompt);
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
        public async Task<IActionResult> GetTopReviewers([FromQuery] int submissionId, [FromQuery] int count = 5)
        {
            try
            {
                var input = new ReviewerSuggestionBySubmissionInputDTO
                {
                    SubmissionId = submissionId,
                    MaxSuggestions = count,
                    UsePrompt = false
                };
                var result = await _reviewerSuggestionService.SuggestReviewersBySubmissionIdAsync(input);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting top reviewers");
                return Error(ConstantModel.ErrorMessage);
            }
        }


        /// <summary>
        /// Gợi ý reviewer cho một chủ đề sử dụng TopicId
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
        [HttpPost("ai-suggest-by-topic")]
        [SwaggerOperation(
            Summary = "AI agent gợi ý reviewer cho một chủ đề",
            Description = "Accessible by Supervisor/Admin/Moderator"
        )]
        [SwaggerResponse(200, "Gợi ý reviewer thành công")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> SuggestByTopicId([FromBody] ReviewerSuggestionByTopicInputDTO input)
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            if (input == null)
                return Error("Dữ liệu đầu vào không hợp lệ");

            try
            {
                var result = await _reviewerSuggestionService.SuggestReviewersByTopicIdAsync(input);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while suggesting reviewers by TopicId");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Kiểm tra reviewer có đủ điều kiện cho một chủ đề sử dụng TopicId không
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
        [HttpGet("check-eligibility-by-topic")]
        [SwaggerOperation(
            Summary = "Kiểm tra reviewer có đủ điều kiện cho một chủ đề",
            Description = "Kiểm tra eligibility dựa trên kỹ năng, workload, hiệu suất, v.v."
        )]
        [SwaggerResponse(200, "Kiểm tra eligibility thành công")]
        [SwaggerResponse(404, "Reviewer không tìm thấy")]
        [Produces("application/json")]
        public async Task<IActionResult> CheckEligibilityByTopicId([FromQuery] int reviewerId, [FromQuery] int topicId)
        {
            try
            {
                var result = await _reviewerSuggestionService.CheckReviewerEligibilityByTopicIdAsync(reviewerId, topicId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking reviewer eligibility by TopicId");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// AI agent suggests reviewers for a submission
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
        [HttpPost("ai-suggest-by-submission")]
        [SwaggerOperation(
            Summary = "AI agent suggests reviewers for a submission",
            Description = "Accessible by Supervisor/Admin/Moderator"
        )]
        [SwaggerResponse(200, "Reviewer suggestions generated successfully")]
        [SwaggerResponse(400, "Invalid input data")]
        [SwaggerResponse(500, "Internal server error")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> SuggestBySubmissionId([FromBody] ReviewerSuggestionBySubmissionInputDTO input)
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            if (input == null)
                return Error("Invalid input data");

            try
            {
                _logger.LogInformation("Processing AI suggestion for SubmissionId: {SubmissionId}", input.SubmissionId);
                _logger.LogInformation("Received input for SuggestBySubmissionId: {@input}", input);
                var result = await _reviewerSuggestionService.SuggestReviewersBySubmissionIdAsync(input);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while suggesting reviewers by SubmissionId: {SubmissionId}", input.SubmissionId);
                _logger.LogError(ex, "Exception details: {Exception}", ex);
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}