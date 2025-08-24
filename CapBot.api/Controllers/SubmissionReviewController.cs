using Microsoft.AspNetCore.Mvc;
using App.BLL.Interfaces;
using App.Entities.DTOs.Review;
using App.Commons.BaseAPI;
using App.Commons;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CapBot.api.Controllers
{
    [Route("api/submission-reviews")]
    [ApiController]
    [Authorize]
    public class SubmissionReviewController : BaseAPIController
    {
        private readonly ISubmissionReviewService _submissionReviewService;
        private readonly ILogger<SubmissionReviewController> _logger;

        public SubmissionReviewController(
            ISubmissionReviewService submissionReviewService,
            ILogger<SubmissionReviewController> logger)
        {
            _submissionReviewService = submissionReviewService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo review cho submission (sử dụng logic 2 reviewer)
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Tạo review cho submission")]
        public async Task<IActionResult> CreateSubmissionReview([FromBody] CreateReviewDTO createDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            try
            {
                var reviewerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _submissionReviewService.CreateSubmissionReviewAsync(createDTO, reviewerId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating submission review");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy tóm tắt review của submission
        /// </summary>
        [HttpGet("{submissionId}/summary")]
        [SwaggerOperation(Summary = "Lấy tóm tắt review của submission")]
        public async Task<IActionResult> GetSubmissionReviewSummary(int submissionId)
        {
            try
            {
                var result = await _submissionReviewService.GetSubmissionReviewSummaryAsync(submissionId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission review summary for ID {SubmissionId}", submissionId);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy danh sách submission xung đột (cần moderator)
        /// </summary>
        [HttpGet("conflicted")]
        [Authorize(Roles = "Moderator,Administrator")]
        [SwaggerOperation(Summary = "Lấy danh sách submission xung đột")]
        public async Task<IActionResult> GetConflictedSubmissions()
        {
            try
            {
                var result = await _submissionReviewService.GetConflictedSubmissionsAsync();
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conflicted submissions");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Moderator quyết định cuối cùng cho submission xung đột
        /// </summary>
        [HttpPost("moderator-final-review")]
        [Authorize(Roles = "Moderator,Administrator")]
        [SwaggerOperation(Summary = "Moderator quyết định cuối cùng")]
        public async Task<IActionResult> ModeratorFinalReview([FromBody] ModeratorFinalReviewDTO moderatorDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            try
            {
                var moderatorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _submissionReviewService.ModeratorFinalReviewAsync(moderatorDTO, moderatorId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in moderator final review");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Đặt deadline cho việc chỉnh sửa
        /// </summary>
        [HttpPost("{reviewId}/set-deadline")]
        [SwaggerOperation(Summary = "Đặt deadline cho việc chỉnh sửa")]
        public async Task<IActionResult> SetRevisionDeadline(int reviewId, [FromBody] DateTime deadline)
        {
            try
            {
                var result = await _submissionReviewService.SetRevisionDeadlineAsync(reviewId, deadline);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting revision deadline for review {ReviewId}", reviewId);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Xử lý submission quá hạn chỉnh sửa (Background job)
        /// </summary>
        [HttpPost("process-overdue")]
        [Authorize(Roles = "Administrator")]
        [SwaggerOperation(Summary = "Xử lý submission quá hạn chỉnh sửa")]
        public async Task<IActionResult> ProcessOverdueRevisions()
        {
            try
            {
                var result = await _submissionReviewService.ProcessOverdueRevisionsAsync();
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing overdue revisions");
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}