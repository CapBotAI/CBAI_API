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
        // Limit concurrent prompt-generation calls to avoid bursting the provider
        private static readonly System.Threading.SemaphoreSlim _promptSemaphore = new System.Threading.SemaphoreSlim(4);

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

                // Send the request with simple retry/backoff for transient failures (429/5xx/network)
                HttpResponseMessage response = null!;
                string responseContent = string.Empty;
                var maxAttempts = 3;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        response = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
                        responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Gemini API response (attempt {attempt}): {responseContent}");

                        if (response.IsSuccessStatusCode)
                            break;

                        // If it's a client error other than 429, bail out immediately
                        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500 && response.StatusCode != (System.Net.HttpStatusCode)429)
                        {
                            throw new Exception($"Gemini Embedding API error: {response.StatusCode} {responseContent}");
                        }
                    }
                    catch (HttpRequestException) when (attempt < maxAttempts)
                    {
                        // transient network error -> retry
                    }

                    // exponential backoff before next attempt
                    if (attempt < maxAttempts)
                        await Task.Delay(500 * attempt);
                }

                if (response == null || !response.IsSuccessStatusCode)
                {
                    throw new Exception($"Gemini Embedding API error after retries: {(response == null ? "no response" : response.StatusCode.ToString())} {responseContent}");
                }

                // Parse the response and try to locate a numeric vector in various possible shapes
                JsonDocument jsonResponse;
                try
                {
                    jsonResponse = JsonDocument.Parse(responseContent);
                }
                catch (JsonException jsonEx)
                {
                    throw new Exception($"Failed to parse Gemini API response: {jsonEx.Message}. Response: {responseContent}");
                }

                // Helper: recursively search JsonElement for the first numeric array (vector)
                float[]? FindNumericArray(JsonElement element, int depth = 0)
                {
                    if (depth > 10) return null; // avoid deep recursion

                    switch (element.ValueKind)
                    {
                        case JsonValueKind.Array:
                            // If array of numbers, return it
                            if (element.EnumerateArray().All(e => e.ValueKind == JsonValueKind.Number))
                            {
                                var list = new List<float>();
                                foreach (var num in element.EnumerateArray())
                                {
                                    // Try parse as single/float/double
                                    try
                                    {
                                        list.Add(num.GetSingle());
                                    }
                                    catch
                                    {
                                        try { list.Add((float)num.GetDouble()); } catch { /* ignore */ }
                                    }
                                }
                                return list.ToArray();
                            }

                            // Otherwise, recurse into array elements
                            foreach (var child in element.EnumerateArray())
                            {
                                var found = FindNumericArray(child, depth + 1);
                                if (found != null && found.Length > 0) return found;
                            }
                            break;

                        case JsonValueKind.Object:
                            // Check common property names first for performance
                            var preferredProps = new[] { "embeddings", "embedding", "values", "vector", "output" };
                            foreach (var prop in preferredProps)
                            {
                                if (element.TryGetProperty(prop, out var propEl))
                                {
                                    var found = FindNumericArray(propEl, depth + 1);
                                    if (found != null && found.Length > 0) return found;
                                }
                            }

                            // Recurse into all properties
                            foreach (var property in element.EnumerateObject())
                            {
                                var found = FindNumericArray(property.Value, depth + 1);
                                if (found != null && found.Length > 0) return found;
                            }
                            break;

                        default:
                            break;
                    }

                    return null;
                }

                var vector = FindNumericArray(jsonResponse.RootElement);
                if (vector == null || vector.Length == 0)
                {
                    throw new Exception($"Could not find numeric embedding vector in Gemini API response. Full response: {responseContent}");
                }

                return vector;
            }
            catch (Exception ex)
            {
                // Log detailed error information
                Console.WriteLine($"Error in GetEmbeddingAsync: {ex.Message}\n{ex}");
                throw new Exception($"Failed to generate embedding for input text. Error: {ex.Message}\nSee inner exception for details.", ex);
            }
        }

        public async Task<string> GetPromptCompletionAsync(string prompt)
        {
            var body = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}"; // Model

            // Ensure we don't overwhelm the provider with parallel requests
            await _promptSemaphore.WaitAsync();
            try
            {
                HttpResponseMessage? resp = null;
                string respContent = string.Empty;
                var maxAttempts = 4;

                // Helper to compute jittered delay
                var rand = new Random();

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        resp = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
                        respContent = await resp.Content.ReadAsStringAsync();

                        if (resp.IsSuccessStatusCode)
                            break;

                        // If client error other than 429, don't retry
                        if ((int)resp.StatusCode >= 400 && (int)resp.StatusCode < 500 && resp.StatusCode != (System.Net.HttpStatusCode)429)
                        {
                            throw new Exception($"Gemini Prompt API error: {resp.StatusCode}");
                        }

                        // If 429, check Retry-After header
                        if (resp.StatusCode == (System.Net.HttpStatusCode)429)
                        {
                            if (resp.Headers.TryGetValues("Retry-After", out var values))
                            {
                                var ra = values.FirstOrDefault();
                                if (int.TryParse(ra, out var seconds))
                                {
                                    // wait the server-suggested delay before retrying
                                    await Task.Delay(TimeSpan.FromSeconds(seconds));
                                    continue;
                                }
                            }
                        }
                    }
                    catch (HttpRequestException) when (attempt < maxAttempts)
                    {
                        // transient network error -> retry
                    }

                    if (attempt < maxAttempts)
                    {
                        // exponential backoff with jitter
                        var backoffMs = Math.Min(5000, (int)(Math.Pow(2, attempt) * 300));
                        var jitter = rand.Next(0, 300);
                        await Task.Delay(backoffMs + jitter);
                    }
                }

                if (resp == null)
                    throw new Exception("No response from Gemini Prompt API.");

                if (!resp.IsSuccessStatusCode)
                {
                    // Provide a concise error but log full content in server logs (caller should log details)
                    throw new Exception($"Gemini Prompt API error after retries: {resp.StatusCode}");
                }

                // Parse response robustly and extract the first textual candidate
                try
                {
                    var doc = JsonDocument.Parse(respContent);
                    var root = doc.RootElement;

                    // Try the expected path first
                    if (root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
                    {
                        var first = candidates[0];
                        if (first.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array && parts.GetArrayLength() > 0)
                        {
                            var textEl = parts[0];
                            if (textEl.ValueKind == JsonValueKind.Object && textEl.TryGetProperty("text", out var textProp))
                                return textProp.GetString() ?? string.Empty;
                        }
                    }

                    // Fallback: search for first string value in the document (depth-first)
                    string? FindFirstString(JsonElement element, int depth = 0)
                    {
                        if (depth > 12) return null;
                        switch (element.ValueKind)
                        {
                            case JsonValueKind.String:
                                return element.GetString();
                            case JsonValueKind.Array:
                                foreach (var el in element.EnumerateArray())
                                {
                                    var found = FindFirstString(el, depth + 1);
                                    if (found != null) return found;
                                }
                                break;
                            case JsonValueKind.Object:
                                foreach (var prop in element.EnumerateObject())
                                {
                                    var found = FindFirstString(prop.Value, depth + 1);
                                    if (found != null) return found;
                                }
                                break;
                            default:
                                break;
                        }
                        return null;
                    }

                    var fallback = FindFirstString(root);
                    return fallback ?? string.Empty;
                }
                catch (JsonException ex)
                {
                    throw new Exception($"Failed to parse Gemini Prompt API response: {ex.Message}. Response omitted for brevity.");
                }
            }
            finally
            {
                _promptSemaphore.Release();
            }
        }
    }
}