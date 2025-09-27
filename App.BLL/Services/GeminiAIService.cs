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
        private readonly string _embeddingModel;
        private readonly string _promptModel;
        private readonly string _region;
        // Limit concurrent prompt-generation calls to avoid bursting the provider
        private static readonly System.Threading.SemaphoreSlim _promptSemaphore = new System.Threading.SemaphoreSlim(4);

        public GeminiAIService(IConfiguration config)
        {
            _apiKey = config["GeminiAI:ApiKey"] ?? throw new ArgumentNullException("GeminiAI:ApiKey missing");
            _embeddingModel = config["GeminiAI:EmbeddingModel"] ?? "gemini-embedding-001";
            _promptModel = config["GeminiAI:PromptModel"] ?? "gemini-1.5-flash";
            _region = config["GeminiAI:Region"] ?? "us-central1";

            _httpClient = new HttpClient();

            // Add API key as a header as some endpoints prefer header-based keys or additional header checks.
            // Also set a simple User-Agent to help with provider logs.
            try
            {
                if (!_httpClient.DefaultRequestHeaders.Contains("x-goog-api-key"))
                    _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
                if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("CapBot/1.0"))
                {
                    // ignore user-agent parse failures
                }
            }
            catch
            {
                // Don't fail construction if headers can't be set; they'll be attempted per-request.
            }
        }

    public async Task<float[]?> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Input text cannot be null or empty.", nameof(text));
            }

            // Use the configured embedding model (supports simple model name or a fully-qualified model path)
            var model = _embeddingModel ?? "gemini-embedding-001";

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

            // Build URL: if a fully-qualified model path was supplied (contains '/'), call that directly.
            string url;
            if (model.Contains("/"))
            {
                url = $"https://{model}:embedContent?key={_apiKey}";
            }
            else
            {
                url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:embedContent?key={_apiKey}";
            }

            try
            {
                // Log the payload being sent
                Console.WriteLine($"Sending payload to Gemini API: {JsonSerializer.Serialize(body)}");

                // Send the request with exponential retry/backoff and jitter for transient failures (429/5xx/network)
                HttpResponseMessage? response = null;
                string responseContent = string.Empty;
                var maxAttempts = 5;
                var rand = new Random();

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        response = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
                        responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Gemini API response (attempt {attempt}): {responseContent}");

                        if (response.IsSuccessStatusCode)
                            break;

                        // If it's a client error other than 429, treat as non-retryable and return null
                        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500 && response.StatusCode != (System.Net.HttpStatusCode)429)
                        {
                            Console.WriteLine($"Gemini Embedding API client error (no-retry): {response.StatusCode}. Response: {responseContent}");
                            return null;
                        }

                        // If 429, respect Retry-After header when present
                        if (response.StatusCode == (System.Net.HttpStatusCode)429)
                        {
                            if (response.Headers.TryGetValues("Retry-After", out var values))
                            {
                                var ra = values.FirstOrDefault();
                                if (int.TryParse(ra, out var seconds))
                                {
                                    await Task.Delay(TimeSpan.FromSeconds(seconds));
                                    continue;
                                }
                            }
                        }

                        // For 5xx (including 503) we will retry up to maxAttempts
                    }
                    catch (HttpRequestException) when (attempt < maxAttempts)
                    {
                        // transient network error -> retry
                    }
                    catch (Exception ex) when (attempt < maxAttempts)
                    {
                        // log and retry for unexpected transient errors
                        Console.WriteLine($"Transient error calling Gemini API (attempt {attempt}): {ex.Message}");
                    }

                    if (attempt < maxAttempts)
                    {
                        // exponential backoff with jitter
                        var backoffMs = Math.Min(10000, (int)(Math.Pow(2, attempt) * 200));
                        var jitter = rand.Next(100, 600);
                        await Task.Delay(backoffMs + jitter);
                    }
                }

                if (response == null || !response.IsSuccessStatusCode)
                {
                    // Service unavailable or repeated failures - degrade gracefully and return null so callers can continue
                    Console.WriteLine($"Gemini Embedding API error after retries: {(response == null ? "no response" : response.StatusCode.ToString())}. Response: {responseContent}");
                    return null;
                }

                // Parse the response and try to locate a numeric vector in various possible shapes
                JsonDocument jsonResponse;
                try
                {
                    jsonResponse = JsonDocument.Parse(responseContent);
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"Failed to parse Gemini API response: {jsonEx.Message}. Response: {responseContent}");
                    return null;
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

                // Normalize embedding to unit length (L2) to make cosine similarity stable across calls
                try
                {
                    double sumSq = 0.0;
                    for (int i = 0; i < vector.Length; i++) sumSq += (double)vector[i] * (double)vector[i];
                    var norm = Math.Sqrt(sumSq);
                    if (norm > 1e-12)
                    {
                        var normalized = new float[vector.Length];
                        for (int i = 0; i < vector.Length; i++) normalized[i] = (float)(vector[i] / norm);

                        // Log a short preview for debugging (first 6 dims)
                        var previewCount = Math.Min(6, normalized.Length);
                        var sb = new System.Text.StringBuilder();
                        for (int i = 0; i < previewCount; i++) { if (i > 0) sb.Append(','); sb.Append(normalized[i].ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                        Console.WriteLine($"Gemini embedding normalized (len={normalized.Length}, norm={norm:F6}) preview=[{sb}]");
                        return normalized;
                    }

                    // if zero-norm, return raw vector
                    return vector;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to normalize embedding: {ex.Message}");
                    return vector;
                }
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

            // Use configured prompt model; allow fully-qualified path or simple model name
            var promptModel = _promptModel ?? "gemini-1.5-flash";
            string url;
            if (promptModel.Contains("/"))
            {
                url = $"https://{promptModel}:generateContent?key={_apiKey}";
            }
            else
            {
                url = $"https://generativelanguage.googleapis.com/v1beta/models/{promptModel}:generateContent?key={_apiKey}";
            } // Model

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
                            var request = new HttpRequestMessage(HttpMethod.Post, url)
                            {
                                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                            };

                            // Ensure API key header exists per-request (some infra blocks query keys)
                            if (!request.Headers.Contains("x-goog-api-key")) request.Headers.Add("x-goog-api-key", _apiKey);
                            if (!request.Headers.UserAgent.TryParseAdd("CapBot/1.0")) { }

                            resp = await _httpClient.SendAsync(request);
                            respContent = await resp.Content.ReadAsStringAsync();

                        if (resp.IsSuccessStatusCode)
                            break;

                        // If client error other than 429, don't retry - treat as non-fatal and return empty so callers can fallback
                        if ((int)resp.StatusCode >= 400 && (int)resp.StatusCode < 500 && resp.StatusCode != (System.Net.HttpStatusCode)429)
                        {
                            // Log details for diagnostics (do not include API keys)
                            Console.WriteLine($"Gemini Prompt API client error (no-retry): {resp.StatusCode}. Response: {respContent}");
                            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                Console.WriteLine("Gemini endpoint returned 404 NotFound - check model name, API key permissions, and whether the project has access to the requested model.");
                            }
                            return string.Empty;
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
                {
                    Console.WriteLine("Gemini Prompt API: no response after retries.");
                    return string.Empty;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    // Log details and return empty so caller can use its fallback logic
                    Console.WriteLine($"Gemini Prompt API error after retries: {resp.StatusCode}. Response: {respContent}");
                    return string.Empty;
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
                    // Log parse failure and return empty so callers can fallback
                    Console.WriteLine($"Failed to parse Gemini Prompt API response: {ex.Message}. Raw response: {respContent}");
                    return string.Empty;
                }
            }
            finally
            {
                _promptSemaphore.Release();
            }
        }

        /// <summary>
        /// Helper: compute cosine similarity between two embeddings (returns double in [-1,1]).
        /// </summary>
        public double CosineSimilarity(float[]? a, float[]? b)
        {
            if (a == null || b == null) return 0.0;
            if (a.Length != b.Length) return 0.0;
            return (double)VectorMath.CosineSimilarity(a, b);
        }
    }
}