using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Entities.DTOs.ReviewerSuggestion
{
    public class ReviewerSuggestionHistoryDTO
    {
        public int TopicVersionId { get; set; }
        public DateTime SuggestedAt { get; set; }
        public List<ReviewerSuggestionDTO> Reviewers { get; set; }
    }
}
