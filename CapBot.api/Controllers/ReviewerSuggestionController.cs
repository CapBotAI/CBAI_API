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
        /// <param name="input">Thông tin gợi ý reviewer</param>
        /// <returns>Danh sách reviewer phù hợp</returns>
        /// <remarks>
        /// - Chỉ Supervisor, Admin hoặc Moderator mới có thể gọi API này.
        /// - Gợi ý reviewer dựa trên kỹ năng, workload, hiệu suất, v.v.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/reviewer-suggestion/suggest
        ///     {
        ///         "topicVersionId": 1,
        ///         "maxSuggestions": 3,
        ///         "usePrompt": true
        ///     }
        /// </remarks>
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
    }
}