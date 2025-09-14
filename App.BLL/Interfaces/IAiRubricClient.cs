using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.BLL.Interfaces
{
    public class RubricEvalResult
    {
        public double OverallScore { get; set; }   // 0..100
        public string OverallRating { get; set; } = "";
        public string RawJson { get; set; } = "";  // full RubricEvaluationResponse JSON
    }

    public interface IAiRubricClient
    {
        Task<RubricEvalResult> EvaluateDocxAsync(
            Stream docxStream,
            string fileName,
            string title,
            int supervisorId,
            int semesterId,
            int? categoryId,
            int maxStudents,
            CancellationToken ct = default
        );
    }
}
