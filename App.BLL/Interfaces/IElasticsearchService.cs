using App.Commons.ResponseModel;
using App.Entities.ElasticModels;
using App.Entities.Entities.App;

namespace App.BLL.Interfaces;

public interface IElasticsearchService
{
    /// <summary>
    /// Index một topic vào Elasticsearch
    /// </summary>
    Task<BaseResponseModel> IndexTopicAsync(Topic topic);

    /// <summary>
    /// Index nhiều topics vào Elasticsearch
    /// </summary>
    Task<BaseResponseModel> BulkIndexTopicsAsync(List<Topic> topics);

    /// <summary>
    /// Tìm kiếm đề tài tương tự
    /// </summary>
    Task<BaseResponseModel<DuplicateDetectionResult>> FindSimilarTopicsAsync(int topicId, double similarityThreshold = 0.5);

    /// <summary>
    /// Tìm kiếm đề tài theo từ khóa
    /// </summary>
    Task<BaseResponseModel<List<TopicDocument>>> SearchTopicsAsync(string query, int size = 10);

    /// <summary>
    /// Xóa topic khỏi index
    /// </summary>
    Task<BaseResponseModel> DeleteTopicFromIndexAsync(int topicId);

    /// <summary>
    /// Cập nhật topic trong index
    /// </summary>
    Task<BaseResponseModel> UpdateTopicInIndexAsync(Topic topic);

    /// <summary>
    /// Tạo index và mapping
    /// </summary>
    Task<BaseResponseModel> CreateIndexAsync();

    /// <summary>
    /// Reindex tất cả topics
    /// </summary>
    Task<BaseResponseModel> ReindexAllTopicsAsync();
}