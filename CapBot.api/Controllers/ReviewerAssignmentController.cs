using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using App.BLL.Interfaces;
using App.Commons.BaseAPI;
using App.Entities.Constants;
using App.Entities.DTOs.ReviewerAssignment;
using App.Entities.Enums;
using App.Commons;

namespace CapBot.api.Controllers;

[Route("api/reviewer-assignments")]
[ApiController]
[Authorize]
public class ReviewerAssignmentController : BaseAPIController
{
    private readonly IReviewerAssignmentService _reviewerAssignmentService;
    private readonly ILogger<ReviewerAssignmentController> _logger;

    public ReviewerAssignmentController(
        IReviewerAssignmentService reviewerAssignmentService,
        ILogger<ReviewerAssignmentController> logger)
    {
        _reviewerAssignmentService = reviewerAssignmentService;
        _logger = logger;
    }

    /// <summary>
    /// Phân công reviewer cho submission
    /// </summary>
    /// <param name="dto">Thông tin phân công reviewer</param>
    /// <returns>Kết quả phân công</returns>
    [HttpPost]
    [Authorize(Roles = $"{SystemRoleConstants.Administrator},{SystemRoleConstants.Moderator}")]
    [SwaggerOperation(
        Summary = "Phân công reviewer cho submission",
        Description = "Cho phép Administrator và Moderator phân công reviewer để review submission"
    )]
    [SwaggerResponse(201, "Phân công thành công")]
    [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
    [SwaggerResponse(401, "Chưa xác thực")]
    [SwaggerResponse(403, "Không có quyền")]
    [SwaggerResponse(404, "Submission hoặc reviewer không tồn tại")]
    [SwaggerResponse(409, "Reviewer đã được phân công cho submission này")]
    public async Task<IActionResult> AssignReviewer([FromBody] AssignReviewerDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return ModelInvalid();
        }

        try
        {
            var result = await _reviewerAssignmentService.AssignReviewerAsync(dto, (int)UserId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while assigning reviewer");
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Phân công nhiều reviewer cùng lúc
    /// </summary>
    /// <param name="dto">Danh sách phân công</param>
    /// <returns>Kết quả phân công hàng loạt</returns>
    [HttpPost("bulk")]
    [Authorize(Roles = $"{SystemRoleConstants.Administrator},{SystemRoleConstants.Moderator}")]
    [SwaggerOperation(
        Summary = "Phân công nhiều reviewer cùng lúc",
        Description = "Cho phép phân công nhiều reviewer cho nhiều submission trong một request"
    )]
    [SwaggerResponse(201, "Phân công thành công")]
    [SwaggerResponse(400, "Một số phân công thất bại")]
    public async Task<IActionResult> BulkAssignReviewers([FromBody] BulkAssignReviewerDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return ModelInvalid();
        }

        try
        {
            var result = await _reviewerAssignmentService.BulkAssignReviewersAsync(dto, (int)UserId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while bulk assigning reviewers");
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Lấy danh sách reviewer có thể phân công
    /// </summary>
    /// <param name="submissionId">ID của submission</param>
    /// <returns>Danh sách reviewer available</returns>
    [HttpGet("available/{submissionId}")]
    [Authorize(Roles = $"{SystemRoleConstants.Administrator},{SystemRoleConstants.Moderator}")]
    [SwaggerOperation(
        Summary = "Lấy danh sách reviewer có thể phân công",
        Description = "Lấy danh sách các reviewer có thể được phân công cho submission, bao gồm thông tin workload"
    )]
    [SwaggerResponse(200, "Lấy danh sách thành công")]
    [SwaggerResponse(404, "Submission không tồn tại")]
    public async Task<IActionResult> GetAvailableReviewers(int submissionId)
    {
        try
        {
            var result = await _reviewerAssignmentService.GetAvailableReviewersAsync(submissionId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting available reviewers for submission {SubmissionId}",
                submissionId);
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Lấy danh sách assignment theo submission
    /// </summary>
    /// <param name="submissionId">ID của submission</param>
    /// <returns>Danh sách assignment</returns>
    [HttpGet("by-submission/{submissionId}")]
    [SwaggerOperation(
        Summary = "Lấy danh sách assignment theo submission",
        Description = "Lấy tất cả reviewer assignments của một submission"
    )]
    [SwaggerResponse(200, "Lấy danh sách thành công")]
    public async Task<IActionResult> GetAssignmentsBySubmission(int submissionId)
    {
        try
        {
            var result = await _reviewerAssignmentService.GetAssignmentsBySubmissionAsync(submissionId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assignments for submission {SubmissionId}",
                submissionId);
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Lấy danh sách assignment của reviewer
    /// </summary>
    /// <param name="reviewerId">ID của reviewer</param>
    /// <returns>Danh sách assignment</returns>
    [HttpGet("by-reviewer/{reviewerId}")]
    [SwaggerOperation(
        Summary = "Lấy danh sách assignment của reviewer",
        Description = "Lấy tất cả assignments được phân công cho một reviewer"
    )]
    [SwaggerResponse(200, "Lấy danh sách thành công")]
    public async Task<IActionResult> GetAssignmentsByReviewer(int reviewerId)
    {
        try
        {
            var result = await _reviewerAssignmentService.GetAssignmentsByReviewerAsync(reviewerId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting assignments for reviewer {ReviewerId}", reviewerId);
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Cập nhật trạng thái assignment
    /// </summary>
    /// <param name="assignmentId">ID của assignment</param>
    /// <param name="status">Trạng thái mới</param>
    /// <returns>Assignment đã cập nhật</returns>
    [HttpPut("{assignmentId}/status")]
    [SwaggerOperation(
        Summary = "Cập nhật trạng thái assignment",
        Description = "Cập nhật trạng thái của một reviewer assignment"
    )]
    [SwaggerResponse(200, "Cập nhật thành công")]
    [SwaggerResponse(404, "Assignment không tồn tại")]
    public async Task<IActionResult> UpdateAssignmentStatus(int assignmentId, [FromBody] AssignmentStatus status)
    {
        try
        {
            var result =
                await _reviewerAssignmentService.UpdateAssignmentStatusAsync(assignmentId, status, (int)UserId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating assignment status {AssignmentId}", assignmentId);
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Hủy assignment
    /// </summary>
    /// <param name="assignmentId">ID của assignment</param>
    /// <returns>Kết quả xóa</returns>
    [HttpDelete("{assignmentId}")]
    [Authorize(Roles = $"{SystemRoleConstants.Administrator},{SystemRoleConstants.Moderator}")]
    [SwaggerOperation(
        Summary = "Hủy assignment",
        Description = "Xóa một reviewer assignment (chỉ được phép nếu chưa có review)"
    )]
    [SwaggerResponse(200, "Xóa thành công")]
    [SwaggerResponse(400, "Không thể xóa assignment đã có review")]
    [SwaggerResponse(404, "Assignment không tồn tại")]
    public async Task<IActionResult> RemoveAssignment(int assignmentId)
    {
        try
        {
            var result = await _reviewerAssignmentService.RemoveAssignmentAsync(assignmentId, (int)UserId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing assignment {AssignmentId}", assignmentId);
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Lấy thống kê workload của reviewers
    /// </summary>
    /// <param name="semesterId">ID học kỳ (tùy chọn)</param>
    /// <returns>Thống kê workload</returns>
    [HttpGet("workload")]
    [Authorize(Roles = $"{SystemRoleConstants.Administrator},{SystemRoleConstants.Moderator}")]
    [SwaggerOperation(
        Summary = "Lấy thống kê workload của reviewers",
        Description = "Lấy thông tin workload hiện tại của tất cả reviewers"
    )]
    [SwaggerResponse(200, "Lấy thống kê thành công")]
    public async Task<IActionResult> GetReviewersWorkload([FromQuery] int? semesterId = null)
    {
        try
        {
            var result = await _reviewerAssignmentService.GetReviewersWorkloadAsync(semesterId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting reviewers workload");
            return Error(ConstantModel.ErrorMessage);
        }
    }
    // Thêm endpoints mới

    /// <summary>
    /// Tự động phân công reviewer dựa trên skill matching
    /// </summary>
    /// <param name="dto">Tiêu chí auto assignment</param>
    /// <returns>Kết quả auto assignment</returns>
    [HttpPost("auto-assign")]
    [Authorize(Roles = $"{SystemRoleConstants.Administrator},{SystemRoleConstants.Moderator}")]
    [SwaggerOperation(
        Summary = "Tự động phân công reviewer dựa trên skill matching",
        Description = "Hệ thống tự động chọn và phân công reviewer phù hợp nhất dựa trên skill, workload và performance"
    )]
    [SwaggerResponse(201, "Auto assign thành công")]
    [SwaggerResponse(400, "Không tìm thấy reviewer phù hợp")]
    [SwaggerResponse(404, "Submission không tồn tại")]
    public async Task<IActionResult> AutoAssignReviewers([FromBody] AutoAssignReviewerDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return ModelInvalid();
        }

        try
        {
            var result = await _reviewerAssignmentService.AutoAssignReviewersAsync(dto, (int)UserId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while auto assigning reviewers for submission {SubmissionId}",
                dto.SubmissionId);
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Lấy danh sách reviewer được recommend
    /// </summary>
    /// <param name="submissionId">ID của submission</param>
    /// <param name="minSkillScore">Minimum skill match score</param>
    /// <param name="maxWorkload">Maximum workload</param>
    /// <returns>Danh sách reviewer được recommend</returns>
    [HttpGet("recommendations/{submissionId}")]
    [Authorize(Roles = $"{SystemRoleConstants.Administrator},{SystemRoleConstants.Moderator}")]
    [SwaggerOperation(
        Summary = "Lấy danh sách reviewer được recommend",
        Description = "Lấy danh sách reviewer được recommend dựa trên skill matching và workload"
    )]
    [SwaggerResponse(200, "Lấy danh sách thành công")]
    [SwaggerResponse(404, "Submission không tồn tại")]
    public async Task<IActionResult> GetRecommendedReviewers(
        int submissionId,
        [FromQuery] decimal minSkillScore = 2.0m,
        [FromQuery] int maxWorkload = 10)
    {
        try
        {
            var criteria = new AutoAssignReviewerDTO
            {
                SubmissionId = submissionId,
                MinimumSkillMatchScore = minSkillScore,
                MaxWorkload = maxWorkload
            };

            var result = await _reviewerAssignmentService.GetRecommendedReviewersAsync(submissionId, criteria);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting recommended reviewers for submission {SubmissionId}",
                submissionId);
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Phân tích matching giữa reviewer và submission
    /// </summary>
    /// <param name="reviewerId">ID của reviewer</param>
    /// <param name="submissionId">ID của submission</param>
    /// <returns>Kết quả phân tích matching</returns>
    [HttpGet("analyze/{reviewerId}/{submissionId}")]
    [SwaggerOperation(
        Summary = "Phân tích matching giữa reviewer và submission",
        Description = "Phân tích chi tiết về độ phù hợp giữa reviewer và submission"
    )]
    [SwaggerResponse(200, "Phân tích thành công")]
    [SwaggerResponse(404, "Reviewer hoặc submission không tồn tại")]
    public async Task<IActionResult> AnalyzeReviewerMatch(int reviewerId, int submissionId)
    {
        try
        {
            var result = await _reviewerAssignmentService.AnalyzeReviewerMatchAsync(reviewerId, submissionId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred while analyzing reviewer match {ReviewerId} for submission {SubmissionId}", reviewerId,
                submissionId);
            return Error(ConstantModel.ErrorMessage);
        }
    }
}