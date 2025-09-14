using App.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App.BLL.Implementations
{
    public class AiRubricClient : IAiRubricClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public AiRubricClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = configuration["Agent:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:8000";
        }

        public async Task<RubricEvalResult> EvaluateDocxAsync(
            Stream docxStream,
            string fileName,
            string title,
            int supervisorId,
            int semesterId,
            int? categoryId,
            int maxStudents,
            CancellationToken ct = default
        )
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_baseUrl}/api/v1/topics/check-rubric-file";

            using var form = new MultipartFormDataContent();
            form.Add(new StreamContent(docxStream), "file", fileName);
            form.Add(new StringContent(title ?? ""), "title");
            form.Add(new StringContent(supervisorId.ToString()), "supervisor_id");
            form.Add(new StringContent(semesterId.ToString()), "semester_id");
            form.Add(new StringContent((categoryId ?? 0).ToString()), "category_id");
            form.Add(new StringContent(maxStudents.ToString()), "max_students");

            var resp = await client.PostAsync(url, form, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var overallScore = root.GetProperty("overall_score").GetDouble();   // 0..100
            var overallRating = root.GetProperty("overall_rating").GetString() ?? "";

            return new RubricEvalResult
            {
                OverallScore = overallScore,
                OverallRating = overallRating,
                RawJson = json
            };
        }
    }
}
