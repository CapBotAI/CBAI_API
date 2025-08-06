using System.Collections.Generic;

namespace App.Entities.DTOs.ReviewerSuggestion
{
    /// <summary>
    /// Output DTO: reviewer suggestions and optional AI explanation.
    /// </summary>
    public class ReviewerSuggestionOutputDTO
    {
        public List<ReviewerSuggestionDTO> Suggestions { get; set; } = new();
        public string? AIExplanation { get; set; }
    }
}