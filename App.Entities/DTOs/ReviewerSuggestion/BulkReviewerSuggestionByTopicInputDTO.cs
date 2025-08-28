using System.Collections.Generic;

namespace App.Entities.DTOs.ReviewerSuggestion
{
    /// <summary>
    /// DTO for bulk reviewer suggestions by TopicId.
    /// </summary>
    public class BulkReviewerSuggestionByTopicInputDTO
    {
        public List<int>? TopicIds { get; set; }
        public int MaxSuggestions { get; set; }
        public bool UsePrompt { get; set; }
    }
}
