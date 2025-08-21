using App.Commons.ResponseModel;
using App.Entities.DTOs.Review;
using App.Commons.Paging;

namespace App.BLL.Interfaces;

public interface ISubmissionReviewService
{
    Task<BaseResponseModel<ReviewResponseDTO>> CreateSubmissionReviewAsync(CreateReviewDTO createDTO, int reviewerId);
    Task<BaseResponseModel<SubmissionReviewSummaryDTO>> GetSubmissionReviewSummaryAsync(int submissionId);
    Task<BaseResponseModel<List<SubmissionReviewSummaryDTO>>> GetConflictedSubmissionsAsync();
    Task<BaseResponseModel<SubmissionReviewSummaryDTO>> ModeratorFinalReviewAsync(ModeratorFinalReviewDTO moderatorDTO, int moderatorId);
    Task<BaseResponseModel> ProcessOverdueRevisionsAsync();
    Task<BaseResponseModel<SubmissionReviewSummaryDTO>> SetRevisionDeadlineAsync(int reviewId, DateTime deadline);
}