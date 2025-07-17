using App.Entities.Entities.App;

namespace App.Entities.DTOs.TopicCategories;

public class TopicCategoryDetailDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int TopicsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    public TopicCategoryDetailDTO() { }

    public TopicCategoryDetailDTO(TopicCategory topicCategory)
    {
        Id = topicCategory.Id;
        Name = topicCategory.Name;
        Description = topicCategory.Description;
        TopicsCount = topicCategory.Topics?.Count ?? 0;
        CreatedAt = topicCategory.CreatedAt;
        CreatedBy = topicCategory.CreatedBy;
        LastModifiedAt = topicCategory.LastModifiedAt;
        LastModifiedBy = topicCategory.LastModifiedBy;
    }
}
