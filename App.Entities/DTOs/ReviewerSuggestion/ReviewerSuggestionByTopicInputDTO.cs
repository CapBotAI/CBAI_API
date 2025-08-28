using System;

namespace App.Entities.DTOs.ReviewerSuggestion
{
    /// <summary>
    /// DTO for reviewer suggestions by TopicId.
    /// </summary>
    public class ReviewerSuggestionByTopicInputDTO
    {
        public int TopicId { get; set; }
        public int MaxSuggestions { get; set; }
        public bool UsePrompt { get; set; }
    }
}