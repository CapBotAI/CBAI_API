using Microsoft.AspNetCore.Mvc;
using App.BLL.Interfaces;
using App.Commons.BaseAPI;
using App.Entities.Entities.App;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace CapBot.api.Controllers
{
    [Route("api/elasticsearch")]
    [ApiController]
    [Authorize]
    public class ElasticsearchController : BaseAPIController
    {
        private readonly IElasticsearchService _elasticsearchService;
        private readonly ILogger<ElasticsearchController> _logger;

        public ElasticsearchController(
            IElasticsearchService elasticsearchService,
            ILogger<ElasticsearchController> logger)
        {
            _elasticsearchService = elasticsearchService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo index và mapping cho Elasticsearch
        /// </summary>
        [HttpPost("create-index")]
        [SwaggerOperation(Summary = "Tạo index Elasticsearch")]
        [SwaggerResponse(200, "Tạo index thành công")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CreateIndex()
        {
            try
            {
                var result = await _elasticsearchService.CreateIndexAsync();
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Elasticsearch index");
                return StatusCode(500, new { message = "Lỗi tạo index" });
            }
        }

        /// <summary>
        /// Reindex tất cả topics
        /// </summary>
        [HttpPost("reindex-all")]
        [SwaggerOperation(Summary = "Reindex tất cả topics")]
        [SwaggerResponse(200, "Reindex thành công")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ReindexAll()
        {
            try
            {
                var result = await _elasticsearchService.ReindexAllTopicsAsync();
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reindexing topics");
                return StatusCode(500, new { message = "Lỗi reindex" });
            }
        }

        /// <summary>
        /// Tìm kiếm đề tài tương tự (phát hiện trùng lặp)
        /// </summary>
        [HttpGet("find-similar/{topicId}")]
        [SwaggerOperation(
            Summary = "Tìm đề tài tương tự",
            Description = "Phát hiện các đề tài có khả năng trùng lặp dựa trên nội dung"
        )]
        [SwaggerResponse(200, "Tìm kiếm thành công")]
        [SwaggerResponse(404, "Topic không tồn tại")]
        public async Task<IActionResult> FindSimilarTopics(
            int topicId,
            [FromQuery] double threshold = 0.5)
        {
            try
            {
                var result = await _elasticsearchService.FindSimilarTopicsAsync(topicId, threshold);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar topics for {TopicId}", topicId);
                return StatusCode(500, new { message = "Lỗi tìm kiếm đề tài tương tự" });
            }
        }

        /// <summary>
        /// Tìm kiếm topics theo từ khóa
        /// </summary>
        [HttpGet("search")]
        [SwaggerOperation(Summary = "Tìm kiếm topics theo từ khóa")]
        [SwaggerResponse(200, "Tìm kiếm thành công")]
        public async Task<IActionResult> SearchTopics(
            [FromQuery] string query,
            [FromQuery] int size = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { message = "Query không được để trống" });
                }

                var result = await _elasticsearchService.SearchTopicsAsync(query, size);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching topics with query: {Query}", query);
                return StatusCode(500, new { message = "Lỗi tìm kiếm" });
            }
        }

        /// <summary>
        /// Index một topic cụ thể
        /// </summary>
        [HttpPost("index-topic/{topicId}")]
        [SwaggerOperation(Summary = "Index một topic vào Elasticsearch")]
        [SwaggerResponse(200, "Index thành công")]
        [SwaggerResponse(404, "Topic không tồn tại")]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> IndexTopic(int topicId)
        {
            try
            {
                var result = await _elasticsearchService.IndexTopicAsync(await GetTopicByIdAsync(topicId));
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing topic {TopicId}", topicId);
                return StatusCode(500, new { message = "Lỗi index topic" });
            }
        }

        /// <summary>
        /// Xóa topic khỏi index
        /// </summary>
        [HttpDelete("delete-topic/{topicId}")]
        [SwaggerOperation(Summary = "Xóa topic khỏi Elasticsearch index")]
        [SwaggerResponse(200, "Xóa thành công")]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> DeleteTopicFromIndex(int topicId)
        {
            try
            {
                var result = await _elasticsearchService.DeleteTopicFromIndexAsync(topicId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting topic {TopicId} from index", topicId);
                return StatusCode(500, new { message = "Lỗi xóa topic khỏi index" });
            }
        }

        #region Private Methods

        private async Task<Topic> GetTopicByIdAsync(int topicId)
        {
            // Logic này sẽ được handle trong service
            // Controller chỉ cần gọi service method
            throw new NotImplementedException("Logic này được handle trong ElasticsearchService");
        }

        #endregion
    }
}