using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace App.BLL.Services
{
    /// <summary>
    /// Calls Gemini API for embeddings and prompt completions.
    /// </summary>
    public class GeminiAIService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GeminiAIService(IConfiguration config)
        {
            _apiKey = config["GeminiAI:ApiKey"] ?? throw new ArgumentNullException("GeminiAI:ApiKey missing");
            _httpClient = new HttpClient();
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            var body = new { instances = new[] { new { content = text } } };
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/embedding-001:embedContent?key={_apiKey}";
            var resp = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Gemini Embedding API error: {resp.StatusCode} {await resp.Content.ReadAsStringAsync()}");
            var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                        .RootElement.GetProperty("predictions")[0].GetProperty("embeddings").EnumerateArray();
            var vector = new List<float>();
            foreach (var v in arr) vector.Add(v.GetSingle());
            return vector.ToArray();
        }

        public async Task<string> GetPromptCompletionAsync(string prompt)
        {
            var body = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={_apiKey}";
            var resp = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Gemini Prompt API error: {resp.StatusCode} {await resp.Content.ReadAsStringAsync()}");
            return JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                   .RootElement.GetProperty("candidates")[0].GetProperty("content")
                   .GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        }
    }
}