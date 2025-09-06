using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Entities.DTOs.Topics
{
    public class TopicDuplicateCheckResDTO
    {
        public int QueryTopicId { get; set; }
        public string QueryTopicTitle { get; set; } = null!;
        public bool IsDuplicate { get; set; }
        public string Message { get; set; } = "topic passed";
        public List<TopicDuplicateItemDTO> Duplicates { get; set; } = new();
    }
}
