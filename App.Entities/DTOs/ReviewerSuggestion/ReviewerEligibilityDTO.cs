using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Entities.DTOs.ReviewerSuggestion
{
    public class ReviewerEligibilityDTO
    {
        public int ReviewerId { get; set; }
        public int TopicVersionId { get; set; }
        public int TopicId { get; set; }
        public bool IsEligible { get; set; }
        public List<string>? Reasons { get; set; }
    }
}
