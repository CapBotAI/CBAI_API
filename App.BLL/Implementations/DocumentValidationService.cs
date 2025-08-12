// App.BLL/Implementations/DocumentValidationService.cs
// Uses: Google_GenerativeAI (NuGet), OpenXML
// Features: retry/backoff + fallback model + full Results + Missing + OverallStatus

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using App.BLL.Interfaces;
using App.Entities.DTOs.DocumentValidation;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

// NuGet: Google_GenerativeAI
using GenerativeAI;
using GenerativeAI.Exceptions;

namespace App.BLL.Implementations
{
    public class DocumentValidationService : IDocumentValidationService
    {
        private readonly IConfiguration _config;

        public DocumentValidationService(IConfiguration config)
        {
            _config = config;
        }

        // Interface method
        public async Task<DocumentValidationResponse> ValidateAsync(IFormFile templateDoc, IFormFile submitDoc)
        {
            if (templateDoc == null || submitDoc == null)
                throw new ArgumentException("Thiếu file template hoặc file submit.");

            // 1) Extract text từ DOCX
            var templateText = await ReadDocxToTextAsync(templateDoc);
            var submitText = await ReadDocxToTextAsync(submitDoc);

            // (Tối ưu) hạn chế độ dài prompt để giảm lỗi 503 & chi phí
            templateText = Truncate(templateText, 30000);
            submitText = Truncate(submitText, 30000);

            // 2) Prompt
            var prompt = BuildPrompt(templateText, submitText);

            // 3) Key & models
            var apiKey = _config["GeminiAI:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Chưa cấu hình Gemini:ApiKey.");

            var preferred = _config["GeminiAI:Model"] ?? "models/gemini-1.5-pro";
            var fallbacks = new[] { preferred, "models/gemini-1.5-flash", "models/gemini-1.5-flash-8b" };

            // 4) Gọi Gemini có retry/backoff + timeout
            var (modelText, usedModel) = await CallGeminiWithRetryAsync(apiKey, fallbacks, prompt);

            // 5) Chuẩn hóa JSON (bỏ ```json ... ```)
            var normalized = NormalizeModelJson(modelText);

            // 6) Parse: fill Results (đầy đủ) + Missing + OverallStatus
            var result = new DocumentValidationResponse
            {
                Model = usedModel,
                RawModelResponse = modelText ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(normalized))
            {
                try
                {
                    using var jd = JsonDocument.Parse(normalized);
                    if (jd.RootElement.TryGetProperty("results", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in arr.EnumerateArray())
                        {
                            var section = item.GetProperty("section").GetString() ?? string.Empty;
                            var status = item.GetProperty("status").GetString() ?? string.Empty;
                            var notes = item.TryGetProperty("notes", out var n) ? (n.GetString() ?? string.Empty) : string.Empty;

                            var row = new TemplateSectionResult
                            {
                                Section = section,
                                Status = status,
                                Notes = notes
                            };

                            // 1) Luôn add vào RESULTS (đầy đủ)
                            result.Results.Add(row);

                            // 2) Nếu là THIẾU, add thêm vào MISSING
                            if (status.Trim().Equals("Thiếu", StringComparison.OrdinalIgnoreCase))
                            {
                                result.Missing.Add(row);
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Không phải JSON hợp lệ => vẫn trả RawModelResponse để debug
                }
            }

            // 7) Trạng thái tổng thể
            result.OverallStatus = (result.Missing.Count == 0) ? "passed" : "failed";

            return result;
        }

        // Overload tiện nếu nơi khác gọi với DTO request
        public Task<DocumentValidationResponse> ValidateAsync(DocumentValidationRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            return ValidateAsync(req.Template, req.Submit);
        }

        #region Helpers
        private static async Task<string> ReadDocxToTextAsync(IFormFile file)
        {
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            using var wordDoc = WordprocessingDocument.Open(ms, false);
            return (wordDoc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty).Trim();
        }

        private static string Truncate(string s, int max)
            => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max));

        private static string BuildPrompt(string templateText, string submitText)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là chuyên gia kiểm định tài liệu. Nhiệm vụ:");
            sb.AppendLine("1) Trích xuất DANH SÁCH CÁC MỤC/PHẦN (và đề mục con nếu có) từ TEMPLATE (Văn bản A).");
            sb.AppendLine("2) Đối chiếu SUBMIT (Văn bản B) với TEMPLATE theo 4 tiêu chí:");
            sb.AppendLine("   - Có đủ tiêu đề mục như trong template hay không.");
            sb.AppendLine("   - Có điền đầy đủ thông tin yêu cầu ở từng mục hay không.");
            sb.AppendLine("   - Nội dung có đúng vị trí và format (bố cục, tiêu đề con) hay không.");
            sb.AppendLine("   - Nếu nội dung có nhưng khác ý nghĩa/khác yêu cầu -> 'Khác nội dung'.");
            sb.AppendLine("3) Trả về JSON duy nhất theo schema:");
            sb.AppendLine("{");
            sb.AppendLine("  \"results\": [");
            sb.AppendLine("     {");
            sb.AppendLine("        \"section\": \"<Tên mục trong template (bao gồm cấp con nếu có)>\",");
            sb.AppendLine("        \"status\":  \"Đầy đủ\" | \"Thiếu\" | \"Sai format\" | \"Khác nội dung\",");
            sb.AppendLine("        \"notes\":   \"Ghi chú chi tiết, nêu chỗ thiếu/sai hoặc khác gì\"");
            sb.AppendLine("     }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("YÊU CẦU:");
            sb.AppendLine("- Đánh giá ở cấp mục rõ ràng (ví dụ: '3. Register content of Capstone Project / Context', '2. Register information for students / Full name', ...).");
            sb.AppendLine("- Ngôn ngữ trả về: tiếng Việt.");
            sb.AppendLine("- Trả về DUY NHẤT một JSON hợp lệ theo schema trên (không kèm ``` hoặc text khác).");
            sb.AppendLine();
            sb.AppendLine("--- VĂN BẢN A (TEMPLATE) ---");
            sb.AppendLine(templateText);
            sb.AppendLine();
            sb.AppendLine("--- VĂN BẢN B (SUBMIT) ---");
            sb.AppendLine(submitText);
            return sb.ToString();
        }

        private static string NormalizeModelJson(string modelText)
        {
            if (string.IsNullOrWhiteSpace(modelText)) return modelText ?? string.Empty;
            var s = modelText.Trim();

            // strip ```json ... ``` or ``` ... ```
            if (s.StartsWith("```"))
            {
                // cắt dòng đầu (``` hoặc ```json)
                var firstNewline = s.IndexOf('\n');
                if (firstNewline >= 0 && firstNewline + 1 < s.Length)
                    s = s.Substring(firstNewline + 1);

                // bỏ 3 backticks cuối nếu còn
                if (s.EndsWith("```"))
                    s = s.Substring(0, s.Length - 3);
            }
            return s.Trim();
        }

        private static async Task<(string Text, string UsedModel)> CallGeminiWithRetryAsync(string apiKey, string[] models, string prompt)
        {
            const int maxAttemptsPerModel = 3;
            var rnd = new Random();

            foreach (var modelName in models)
            {
                var googleAI = new GoogleAi(apiKey);
                var model = googleAI.CreateGenerativeModel(modelName);

                for (int attempt = 1; attempt <= maxAttemptsPerModel; attempt++)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    try
                    {
                        var resp = await model.GenerateContentAsync(prompt, cts.Token);
                        var text = resp?.Text() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(text))
                            return (text, modelName);

                        // empty response -> retry
                        var delayEmpty = (int)Math.Min(8000, Math.Pow(2, attempt) * 250) + rnd.Next(100, 400);
                        await Task.Delay(delayEmpty);
                    }
                    catch (ApiException ex) when (ex.ErrorCode == 503 // 
                                               || ex.ErrorCode == 429)          // Too Many Requests
                    {
                        var delayMs = (int)Math.Min(8000, Math.Pow(2, attempt) * 250) + rnd.Next(100, 400);
                        await Task.Delay(delayMs);
                        // continue retry
                    }
                    catch (TaskCanceledException)
                    {
                        var delayMs = (int)Math.Min(8000, Math.Pow(2, attempt) * 250) + rnd.Next(100, 400);
                        await Task.Delay(delayMs);
                    }
                }
                // try next model
            }

            throw new InvalidOperationException("Gemini đang quá tải hoặc không phản hồi. Vui lòng thử lại sau (đã retry & fallback).");
        }
        #endregion
    }
}
