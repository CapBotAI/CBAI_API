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
        private readonly App.BLL.Interfaces.IReviewerAssignmentService _reviewerAssignmentService;
        private readonly ILogger<ReviewerSuggestionController> _logger;

        public ReviewerSuggestionController(
            IReviewerSuggestionService reviewerSuggestionService,
            App.BLL.Interfaces.IReviewerAssignmentService reviewerAssignmentService,
            ILogger<ReviewerSuggestionController> logger)
        {
            _reviewerSuggestionService = reviewerSuggestionService;
            _reviewerAssignmentService = reviewerAssignmentService;
            _logger = logger;
        }

        /// <summary>
        /// Auto-assign all eligible reviewers from a suggestion output JSON
        /// </summary>
        [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
        [HttpPost("auto-assign-from-suggestions")]
        [SwaggerOperation(
            Summary = "Tự động phân công các reviewer được gợi ý",
            Description = "Nhận JSON suggestions và tạo assignment cho tất cả reviewer phù hợp"
        )]
        [SwaggerResponse(201, "Auto assign thành công")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        public async Task<IActionResult> AutoAssignFromSuggestions([FromBody] App.Entities.DTOs.ReviewerSuggestion.ReviewerSuggestionOutputDTO suggestionOutput, [FromQuery] int submissionId)
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            if (suggestionOutput == null || suggestionOutput.Suggestions == null || !suggestionOutput.Suggestions.Any())
                return Error("Không có reviewer được gợi ý để phân công");

            try
            {
                var assignments = suggestionOutput.Suggestions
                    .Where(s => s.IsEligible)
                    .Select(s => new App.Entities.DTOs.ReviewerAssignment.AssignReviewerDTO
                    {
                        SubmissionId = submissionId,
                        ReviewerId = s.ReviewerId,
                        AssignmentType = App.Entities.Enums.AssignmentTypes.Primary,
                        SkillMatchScore = s.SkillMatchScore
                    })
                    .ToList();

                if (!assignments.Any())
                    return Error("Không có reviewer phù hợp để phân công");

                var bulkDto = new App.Entities.DTOs.ReviewerAssignment.BulkAssignReviewerDTO
                {
                    Assignments = assignments
                };

                var reviewerAssignmentService = HttpContext.RequestServices.GetService(typeof(App.BLL.Interfaces.IReviewerAssignmentService)) as App.BLL.Interfaces.IReviewerAssignmentService;
                if (reviewerAssignmentService == null)
                    return Error(ConstantModel.ErrorMessage);

                var result = await reviewerAssignmentService.BulkAssignReviewersAsync(bulkDto, (int)UserId);
                if (result == null)
                    return Error(ConstantModel.ErrorMessage);

                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while auto assigning reviewers from suggestions");
                return Error(ConstantModel.ErrorMessage);
            }
        }

            /// <summary>
            /// Assign suggested reviewers (from suggestion output) to a submission
            /// </summary>
            [Authorize(Roles = SystemRoleConstants.Supervisor + "," + SystemRoleConstants.Administrator + "," + SystemRoleConstants.Moderator)]
            [HttpPost("assign-from-suggestions")]
            [SwaggerOperation(
                Summary = "Gán các reviewer được gợi ý cho submission",
                Description = "Nhận một danh sách reviewer được gợi ý và tạo các ReviewerAssignment tương ứng"
            )]
            [SwaggerResponse(201, "Phân công thành công")]
            [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
            public async Task<IActionResult> AssignFromSuggestions([FromBody] App.Entities.DTOs.ReviewerAssignment.AssignFromSuggestionDTO dto)
            {
                if (!ModelState.IsValid)
                    return ModelInvalid();

                if (dto == null || dto.Reviewers == null || !dto.Reviewers.Any())
                    return Error("Không có reviewer để phân công");

                try
                {
                    // Map to BulkAssignReviewerDTO
                    var bulkDto = new App.Entities.DTOs.ReviewerAssignment.BulkAssignReviewerDTO
                    {
                        Assignments = dto.Reviewers.Select(r => new App.Entities.DTOs.ReviewerAssignment.AssignReviewerDTO
                        {
                            SubmissionId = dto.SubmissionId,
                            ReviewerId = r.ReviewerId,
                            AssignmentType = r.AssignmentType ?? App.Entities.Enums.AssignmentTypes.Primary,
                            Deadline = r.Deadline,
                            SkillMatchScore = r.SkillMatchScore
                        }).ToList()
                    };

                    // Resolve service via DI
                    var reviewerAssignmentService = HttpContext.RequestServices.GetService(typeof(App.BLL.Interfaces.IReviewerAssignmentService)) as App.BLL.Interfaces.IReviewerAssignmentService;
                    if (reviewerAssignmentService == null)
                        return Error(ConstantModel.ErrorMessage);

                    var result = await reviewerAssignmentService.BulkAssignReviewersAsync(bulkDto, (int)UserId);
                    return ProcessServiceResponse(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while assigning reviewers from suggestions");
                    return Error(ConstantModel.ErrorMessage);
                }
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
    public async Task<IActionResult> SuggestBySubmissionId([FromBody] ReviewerSuggestionBySubmissionInputDTO input, [FromQuery] bool assign = false)
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

                // If caller requested auto-assign, map eligible suggestions to assignments and use the assignment service
                if (assign && result != null && result.IsSuccess && result.Data != null && result.Data.Suggestions != null && result.Data.Suggestions.Any())
                {
                    try
                    {
                        // Attempt to assign every suggested reviewer (UI expects all ids to be processed)
                        var assignments = result.Data.Suggestions.Select(s => new App.Entities.DTOs.ReviewerAssignment.AssignReviewerDTO
                        {
                            SubmissionId = input.SubmissionId,
                            ReviewerId = s.ReviewerId,
                            AssignmentType = App.Entities.Enums.AssignmentTypes.Primary,
                            SkillMatchScore = s.SkillMatchScore,
                            Deadline = input.Deadline
                        }).ToList();

                        // Perform assignments one-by-one to ensure each suggested reviewer is attempted
                        var assignResults = new List<App.Entities.DTOs.ReviewerAssignment.ReviewerAssignmentResponseDTO>();
                        var assignErrors = new List<string>();

                        foreach (var a in assignments)
                        {
                            try
                            {
                                var singleResult = await _reviewerAssignmentService.AssignReviewerAsync(a, (int)UserId);
                                if (singleResult != null && singleResult.IsSuccess)
                                {
                                    if (singleResult.Data != null)
                                        assignResults.Add(singleResult.Data);
                                }
                                else
                                {
                                    var msg = singleResult?.Message ?? "Unknown error";
                                    assignErrors.Add($"Reviewer {a.ReviewerId}: {msg}");
                                }
                            }
                            catch (Exception exAssign)
                            {
                                _logger.LogError(exAssign, "Error assigning reviewer {ReviewerId}", a.ReviewerId);
                                assignErrors.Add($"Reviewer {a.ReviewerId}: Exception during assign");
                            }
                        }

                        result.Data.AssignmentResults = assignResults;
                        result.Data.AssignmentErrors = assignErrors;
                        if (assignErrors.Any())
                        {
                            result.Message += "; Auto-assign: " + string.Join("; ", assignErrors);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while auto-assigning reviewers after suggestion");
                        result.Message += "; Auto-assign failed";
                    }
                }

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