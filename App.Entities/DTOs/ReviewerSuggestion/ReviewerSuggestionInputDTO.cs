using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Entities.DTOs.ReviewerSuggestion
{
    /// <summary>
    /// Input DTO for requesting reviewer suggestions.
    /// </summary>
    public class ReviewerSuggestionInputDTO
    {
        public int TopicVersionId { get; set; }
        public int MaxSuggestions { get; set; } = 3;
        public bool UsePrompt { get; set; } = false;// If true, return Gemini AI explanation
    }
}
