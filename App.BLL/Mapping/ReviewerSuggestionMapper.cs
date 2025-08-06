using System.Collections.Generic;
using App.Entities.DTOs.ReviewerSuggestion;

namespace App.BLL.Mapping
{
    /// <summary>
    /// Maps reviewer suggestion results to the output DTO structure.
    /// </summary>
    public static class ReviewerSuggestionMapper
    {
        public static ReviewerSuggestionOutputDTO ToOutputDTO(
            List<ReviewerSuggestionDTO> reviewers,
            string? aiExplanation = null)
        {
            return new ReviewerSuggestionOutputDTO
            {
                Suggestions = reviewers,
                AIExplanation = aiExplanation
            };
        }
    }
}