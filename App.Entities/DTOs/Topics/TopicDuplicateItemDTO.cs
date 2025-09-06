using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Entities.DTOs.Topics
{
    public class TopicDuplicateItemDTO
    {
        public int TopicId { get; set; }
        public string Title { get; set; } = null!;
        public string? SemesterName { get; set; }
        public string? SupervisorName { get; set; }
        public double SimilarityScore { get; set; }
        public string SimilarityPercentage => $"{SimilarityScore * 100:F2}%";
    }
}
