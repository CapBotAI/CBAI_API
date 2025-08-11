using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Entities.DTOs.ReviewerSuggestion
{
    public class BulkReviewerSuggestionInputDTO
    {
        public List<int> TopicVersionIds { get; set; }
        public int MaxSuggestions { get; set; }
        public bool UsePrompt { get; set; }
    }
}
