using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.UnitOfWork;
using App.Entities.ElasticModels;
using App.Entities.Entities.App;
using Microsoft.AspNetCore.Http;
using Elastic.Clients.Elasticsearch;
using System.Text;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace App.BLL.Implementations;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _elasticClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _indexName = "topics";

    public ElasticsearchService(ElasticsearchClient elasticClient, IUnitOfWork unitOfWork)
    {
        _elasticClient = elasticClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponseModel> CreateIndexAsync()
    {
        try
        {
            // Kiểm tra index có tồn tại không
            var existsResponse = await _elasticClient.Indices.ExistsAsync(_indexName);
            if (existsResponse.Exists)
            {
                await _elasticClient.Indices.DeleteAsync(_indexName);
            }

            // Tạo index mới với mapping đơn giản
            var createResponse = await _elasticClient.Indices.CreateAsync(_indexName, c => c
                .Mappings(m => m
                    .Properties<TopicDocument>(p => p
                        .IntegerNumber(n => n.Id)
                        .Text(t => t.Title)
                        .Text(t => t.Description)
                        .Text(t => t.Objectives)
                        .IntegerNumber(n => n.SupervisorId)
                        .IntegerNumber(n => n.CategoryId)
                        .IntegerNumber(n => n.SemesterId)
                        .IntegerNumber(n => n.MaxStudents)
                        .Boolean(b => b.IsLegacy)
                        .Boolean(b => b.IsApproved)
                        .Text(t => t.CategoryName)
                        .Text(t => t.SemesterName)
                        .Text(t => t.SupervisorName)
                        .Date(d => d.CreatedAt)
                        .Date(d => d.LastModifiedAt)
                        .Boolean(b => b.IsActive)
                        .Text(t => t.FullContent)
                        .Keyword(k => k.Keywords)
                    )
                )
            );

            if (!createResponse.IsValidResponse)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi tạo index: {createResponse.DebugInformation}"
                };
            }

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Tạo index thành công"
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel> IndexTopicAsync(Topic topic)
    {
        try
        {
            var document = MapTopicToDocument(topic);

            var response = await _elasticClient.IndexAsync(document, _indexName);

            if (!response.IsValidResponse)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi index topic: {response.DebugInformation}"
                };
            }

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Index topic thành công"
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel> BulkIndexTopicsAsync(List<Topic> topics)
    {
        try
        {
            var documents = topics.Select(MapTopicToDocument).ToList();

            var bulkResponse = await _elasticClient.BulkAsync(b => b
                .Index(_indexName)
                .IndexMany(documents)
            );

            if (!bulkResponse.IsValidResponse)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi bulk index: {bulkResponse.DebugInformation}"
                };
            }

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Bulk index {documents.Count} topics thành công"
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<DuplicateDetectionResult>> FindSimilarTopicsAsync(int topicId, double similarityThreshold = 0.5)
    {
        try
        {
            // Lấy topic gốc từ database
            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var sourceTopic = await topicRepo.GetSingleAsync(new DAL.Queries.QueryOptions<Topic>
            {
                Predicate = t => t.Id == topicId && t.IsActive,
                IncludeProperties = new List<System.Linq.Expressions.Expression<Func<Topic, object>>>
                {
                    t => t.Category,
                    t => t.Semester,
                    t => t.Supervisor
                }
            });

            if (sourceTopic == null)
            {
                return new BaseResponseModel<DuplicateDetectionResult>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Topic không tồn tại"
                };
            }

            // Tạo query content
            var queryContent = BuildSearchContent(sourceTopic);

            // Tìm kiếm similar topics với Multi Match query
            var searchResponse = await _elasticClient.SearchAsync<TopicDocument>(s => s
                .Index(_indexName)
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .MultiMatch(mm => mm
                                .Query(queryContent)
                                .Fields(new[] { "full_content^1.0", "title^2.0", "description^1.5" })
                                .Type(TextQueryType.BestFields)
                                .MinimumShouldMatch("30%")
                            )
                        )
                        .MustNot(mn => mn
                            .Term(t => t
                                .Field("id")
                                .Value(topicId)
                            )
                        )
                    )
                )
                .Size(20)
                .MinScore((float)similarityThreshold)
            );

            if (!searchResponse.IsValidResponse)
            {
                return new BaseResponseModel<DuplicateDetectionResult>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi tìm kiếm: {searchResponse.DebugInformation}"
                };
            }

            var similarTopics = searchResponse.Documents.Select(doc =>
            {
                var hit = searchResponse.Hits.First(h => h.Source?.Id == doc.Id);
                return new SimilarityResult
                {
                    TopicId = doc.Id,
                    Title = doc.Title,
                    Description = doc.Description,
                    SimilarityScore = hit.Score ?? 0,
                    MatchedFields = GetMatchedFields(doc, sourceTopic),
                    SupervisorName = doc.SupervisorName,
                    SemesterName = doc.SemesterName,
                    CreatedAt = doc.CreatedAt
                };
            }).OrderByDescending(s => s.SimilarityScore).ToList();

            var result = new DuplicateDetectionResult
            {
                QueryTopicId = topicId,
                QueryTopicTitle = sourceTopic.EN_Title,
                SimilarTopics = similarTopics,
                HighestSimilarity = similarTopics.FirstOrDefault()?.SimilarityScore ?? 0,
                DetectionSummary = GenerateDetectionSummary(similarTopics)
            };

            return new BaseResponseModel<DuplicateDetectionResult>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Tìm thấy {similarTopics.Count} đề tài tương tự",
                Data = result
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<DuplicateDetectionResult>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<List<TopicDocument>>> SearchTopicsAsync(string query, int size = 10)
    {
        try
        {
            var searchResponse = await _elasticClient.SearchAsync<TopicDocument>(s => s
                .Index(_indexName)
                .Query(q => q
                    .Bool(b => b
                        .Should(
                            sh => sh.Match(m => m
                                .Field("title")
                                .Query(query)
                                .Boost(2.0f)
                            ),
                            sh => sh.Match(m => m
                                .Field("description")
                                .Query(query)
                            ),
                            sh => sh.Match(m => m
                                .Field("full_content")
                                .Query(query)
                            ),
                            sh => sh.Wildcard(w => w
                                .Field("title")
                                .Value($"*{query.ToLower()}*")
                            )
                        )
                        .Must(m => m
                            .Term(t => t
                                .Field("is_active")
                                .Value(true)
                            )
                        )
                    )
                )
                .Size(size)
            );

            if (!searchResponse.IsValidResponse)
            {
                return new BaseResponseModel<List<TopicDocument>>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi tìm kiếm: {searchResponse.DebugInformation}"
                };
            }

            return new BaseResponseModel<List<TopicDocument>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Tìm thấy {searchResponse.Documents.Count} kết quả",
                Data = searchResponse.Documents.ToList()
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<List<TopicDocument>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel> DeleteTopicFromIndexAsync(int topicId)
    {
        try
        {
            var response = await _elasticClient.DeleteAsync<TopicDocument>(topicId, d => d.Index(_indexName));

            return new BaseResponseModel
            {
                IsSuccess = response.IsValidResponse,
                StatusCode = response.IsValidResponse ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError,
                Message = response.IsValidResponse ? "Xóa topic khỏi index thành công" : $"Lỗi xóa: {response.DebugInformation}"
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel> UpdateTopicInIndexAsync(Topic topic)
    {
        try
        {
            var document = MapTopicToDocument(topic);

            var response = await _elasticClient.UpdateAsync<TopicDocument, TopicDocument>(_indexName, topic.Id, u => u
                .Doc(document)
                .DocAsUpsert()
            );

            return new BaseResponseModel
            {
                IsSuccess = response.IsValidResponse,
                StatusCode = response.IsValidResponse ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError,
                Message = response.IsValidResponse ? "Cập nhật topic trong index thành công" : $"Lỗi cập nhật: {response.DebugInformation}"
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel> ReindexAllTopicsAsync()
    {
        try
        {
            // Tạo index mới
            var createResult = await CreateIndexAsync();
            if (!createResult.IsSuccess)
            {
                return createResult;
            }

            // Lấy tất cả topics từ database
            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var topics = await topicRepo.GetAllAsync(new DAL.Queries.QueryOptions<Topic>
            {
                Predicate = t => t.IsActive,
                IncludeProperties = new List<System.Linq.Expressions.Expression<Func<Topic, object>>>
                {
                    t => t.Category,
                    t => t.Semester,
                    t => t.Supervisor
                }
            });

            // Bulk index
            var result = await BulkIndexTopicsAsync(topics.ToList());
            return result;
        }
        catch (Exception ex)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi reindex: {ex.Message}"
            };
        }
    }

    #region Private Methods

    private TopicDocument MapTopicToDocument(Topic topic)
    {
        var fullContent = BuildFullContent(topic);
        var keywords = ExtractKeywords(topic);

        return new TopicDocument
        {
            Id = topic.Id,
            Title = topic.EN_Title,
            Description = topic.Description,
            Objectives = topic.Objectives,
            SupervisorId = topic.SupervisorId,
            CategoryId = topic.CategoryId ?? 0, // Handle nullable int
            SemesterId = topic.SemesterId,
            MaxStudents = topic.MaxStudents,
            IsLegacy = topic.IsLegacy,
            IsApproved = topic.IsApproved,
            CategoryName = topic.Category?.Name,
            SemesterName = topic.Semester?.Name,
            SupervisorName = topic.Supervisor?.UserName,
            CreatedAt = topic.CreatedAt,
            LastModifiedAt = topic.LastModifiedAt,
            IsActive = topic.IsActive,
            FullContent = fullContent,
            Keywords = keywords
        };
    }

    private string BuildFullContent(Topic topic)
    {
        var content = new StringBuilder();
        content.AppendLine(topic.EN_Title);
        if (!string.IsNullOrEmpty(topic.Description)) content.AppendLine(topic.Description);
        if (!string.IsNullOrEmpty(topic.Objectives)) content.AppendLine(topic.Objectives);

        return content.ToString();
    }

    private string BuildSearchContent(Topic topic)
    {
        return BuildFullContent(topic);
    }

    private List<string> ExtractKeywords(Topic topic)
    {
        var keywords = new List<string>();

        // Extract từ title
        if (!string.IsNullOrEmpty(topic.EN_Title))
        {
            keywords.AddRange(topic.EN_Title.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        // Extract từ objectives
        if (!string.IsNullOrEmpty(topic.Objectives))
        {
            keywords.AddRange(topic.Objectives.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3));
        }

        return keywords.Distinct().Where(k => k.Length > 2).ToList();
    }

    private List<string> GetMatchedFields(TopicDocument similarTopic, Topic sourceTopic)
    {
        var matchedFields = new List<string>();

        if (IsTextSimilar(similarTopic.Title, sourceTopic.EN_Title))
            matchedFields.Add("Title");

        if (IsTextSimilar(similarTopic.Description, sourceTopic.Description))
            matchedFields.Add("Description");

        if (IsTextSimilar(similarTopic.Objectives, sourceTopic.Objectives))
            matchedFields.Add("Objectives");

        return matchedFields;
    }

    private bool IsTextSimilar(string? text1, string? text2, double threshold = 0.6)
    {
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return false;

        var similarity = CalculateJaccardSimilarity(text1, text2);
        return similarity >= threshold;
    }

    private double CalculateJaccardSimilarity(string text1, string text2)
    {
        var set1 = text1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var set2 = text2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        var intersection = set1.Intersect(set2).Count();
        var union = set1.Union(set2).Count();

        return union == 0 ? 0 : (double)intersection / union;
    }

    private string GenerateDetectionSummary(List<SimilarityResult> similarTopics)
    {
        if (!similarTopics.Any())
            return "Không tìm thấy đề tài tương tự";

        var highSimilarity = similarTopics.Count(s => s.SimilarityScore > 0.8);
        var mediumSimilarity = similarTopics.Count(s => s.SimilarityScore > 0.6 && s.SimilarityScore <= 0.8);
        var lowSimilarity = similarTopics.Count(s => s.SimilarityScore <= 0.6);

        var summary = new StringBuilder();
        summary.AppendLine($"Phát hiện {similarTopics.Count} đề tài tương tự:");

        if (highSimilarity > 0)
            summary.AppendLine($"- {highSimilarity} đề tài có độ tương tự cao (>80%) - CẦN KIỂM TRA");
        if (mediumSimilarity > 0)
            summary.AppendLine($"- {mediumSimilarity} đề tài có độ tương tự trung bình (60-80%)");
        if (lowSimilarity > 0)
            summary.AppendLine($"- {lowSimilarity} đề tài có độ tương tự thấp (<60%)");

        return summary.ToString();
    }

    #endregion
}