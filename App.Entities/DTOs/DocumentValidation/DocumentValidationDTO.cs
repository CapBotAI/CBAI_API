using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Entities.DTOs.DocumentValidation
{

    public class TemplateSectionResult
    {
        public string Section { get; set; } = "";
        public string Status { get; set; } = ""; // Đầy đủ / Thiếu / Sai format / Khác nội dung
        public string Notes { get; set; } = "";
    }

    public class DocumentValidationResponse
    {
        public string Model { get; set; } = "gemini-1.5-pro";
        public List<TemplateSectionResult> Results { get; set; } = new(); // ĐẦY ĐỦ
        public List<TemplateSectionResult> Missing { get; set; } = new(); // CHỈ MỤC THIẾU
        public string OverallStatus { get; set; } = ""; // passed / failed
        public int MissingCount => Missing?.Count ?? 0;  // tiện FE
        public string RawModelResponse { get; set; } = ""; // để debug khi cần
    }
    public class DocumentValidationRequest
    {
        public IFormFile Template { get; set; } = default!;
        public IFormFile Submit { get; set; } = default!;
    }
}


