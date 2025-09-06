using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using App.BLL.Interfaces;
using Microsoft.Extensions.Configuration;

namespace App.BLL.Implementations;

public class GeminiService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<string>> GenerateKeywordsAsync(string title, string? description, int maxKeywords = 20, CancellationToken ct = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
            return new List<string>();

        var prompt = BuildPrompt(title, description, maxKeywords);
        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };
        using var resp = await _httpClient.SendAsync(httpReq, ct);

        if (!resp.IsSuccessStatusCode)
            return new List<string>();

        var json = await resp.Content.ReadAsStringAsync(ct);
        var keywords = ParseKeywords(json);
        return keywords;
    }

    private static string BuildPrompt(string title, string? description, int maxKeywords)
    {
        var content = $"{title}\n{description}".Trim();
        return $@"Bạn là trợ lý NLP. Hãy trích xuất danh sách từ khóa ngắn (1-3 từ) liên quan nhất từ tiêu đề và mô tả sau.
- Trả về CHỈ DUY NHẤT một JSON array các chuỗi, không kèm giải thích.
- Tối đa {maxKeywords} từ khóa, ưu tiên domain học thuật, loại bỏ stopwords, ký tự đặc biệt.

Nội dung:
{content}";
    }

    private static List<string> ParseKeywords(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            // Đọc text trả về từ Gemini
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            // Cố gắng parse JSON array
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                // Xóa code fence nếu có
                var start = text.IndexOf('[');
                var end = text.LastIndexOf(']');
                if (start >= 0 && end >= start)
                    text = text.Substring(start, end - start + 1);
            }

            var arr = JsonSerializer.Deserialize<List<string>>(text);
            return arr?
                       .Select(k => k?.Trim())
                       .Where(k => !string.IsNullOrWhiteSpace(k))
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList()
                   ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}