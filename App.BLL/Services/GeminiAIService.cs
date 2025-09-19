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
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Input text cannot be null or empty.", nameof(text));
            }

            // Use the correct embedding model
            var model = "gemini-embedding-001";

            // Adjust payload structure for embedding requests
            var body = new
            {
                model = model,
                content = new
                {
                    parts = new[]
                    {
                        new { text = text }
                    }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:embedContent?key={_apiKey}";

            try
            {
                // Log the payload being sent
                Console.WriteLine($"Sending payload to Gemini API: {JsonSerializer.Serialize(body)}");

                // Send the request
                var response = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

                // Log the raw response
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Gemini API response: {responseContent}");

                // Check for success
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Gemini Embedding API error: {response.StatusCode} {responseContent}");
                }

                // Parse the response with additional error handling
                JsonDocument jsonResponse;
                try
                {
                    jsonResponse = JsonDocument.Parse(responseContent);
                }
                catch (JsonException jsonEx)
                {
                    throw new Exception($"Failed to parse Gemini API response: {jsonEx.Message}. Response: {responseContent}");
                }

                if (!jsonResponse.RootElement.TryGetProperty("predictions", out var predictions) || predictions.GetArrayLength() == 0)
                {
                    throw new Exception($"Missing or empty 'predictions' in Gemini API response: {responseContent}");
                }

                var firstPrediction = predictions[0];
                if (!firstPrediction.TryGetProperty("embeddings", out var embeddings))
                {
                    throw new Exception($"Missing 'embeddings' in the first prediction of Gemini API response: {responseContent}");
                }

                var vector = new List<float>();
                foreach (var value in embeddings.EnumerateArray())
                {
                    vector.Add(value.GetSingle());
                }

                return vector.ToArray();
            }
            catch (Exception ex)
            {
                // Log detailed error information
                Console.WriteLine($"Error in GetEmbeddingAsync: {ex.Message}");
                throw new Exception($"Failed to generate embedding for input text. Error: {ex.Message}", ex);
            }
        }

        public async Task<string> GetPromptCompletionAsync(string prompt)
        {
            var body = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}"; // Updated to gemini-1.5-flash model
            var resp = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Gemini Prompt API error: {resp.StatusCode} {await resp.Content.ReadAsStringAsync()}");
            return JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                   .RootElement.GetProperty("candidates")[0].GetProperty("content")
                   .GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        }
    }
}