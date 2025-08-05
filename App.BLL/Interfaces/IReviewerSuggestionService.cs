using System.Threading.Tasks;
using App.Entities.DTOs.ReviewerSuggestion;

namespace App.BLL.Interfaces
{
    /// <summary>
    /// Service contract for reviewer suggestion logic.
    /// </summary>
    public interface IReviewerSuggestionService
    {
        Task<ReviewerSuggestionOutputDTO> SuggestReviewersAsync(ReviewerSuggestionInputDTO input);
    }
}