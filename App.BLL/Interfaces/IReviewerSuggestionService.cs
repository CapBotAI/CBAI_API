using System.Threading.Tasks;
using App.Commons.ResponseModel;
using App.Entities.DTOs.ReviewerSuggestion;

namespace App.BLL.Interfaces
{
    /// <summary>
    /// Service contract for reviewer suggestion logic.
    /// </summary>
    public interface IReviewerSuggestionService
    {
        Task<BaseResponseModel<ReviewerSuggestionOutputDTO>> SuggestReviewersAsync(ReviewerSuggestionInputDTO input);
        Task<BaseResponseModel<ReviewerEligibilityDTO>> CheckReviewerEligibilityAsync(int reviewerId, int topicVersionId);
        Task<BaseResponseModel<ReviewerSuggestionOutputDTO>> SuggestReviewersByTopicIdAsync(ReviewerSuggestionByTopicInputDTO input);
        Task<BaseResponseModel<ReviewerEligibilityDTO>> CheckReviewerEligibilityByTopicIdAsync(int reviewerId, int topicId);
        Task<BaseResponseModel<ReviewerSuggestionOutputDTO>> SuggestReviewersBySubmissionIdAsync(ReviewerSuggestionBySubmissionInputDTO input);
    }
}