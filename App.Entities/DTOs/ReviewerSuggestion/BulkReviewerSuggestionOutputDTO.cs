using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Entities.DTOs.ReviewerSuggestion
{
    public class BulkReviewerSuggestionOutputDTO
    {
        public int TopicVersionId { get; set; }
        public int TopicId { get; set; }
        public ReviewerSuggestionOutputDTO? Suggestion { get; set; }
    }
}
