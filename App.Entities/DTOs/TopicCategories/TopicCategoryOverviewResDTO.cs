using App.Entities.Entities.App;

namespace App.Entities.DTOs.TopicCategories;

public class TopicCategoryOverviewResDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int TopicsCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public TopicCategoryOverviewResDTO() { }

    public TopicCategoryOverviewResDTO(TopicCategory topicCategory)
    {
        Id = topicCategory.Id;
        Name = topicCategory.Name;
        Description = topicCategory.Description;
        TopicsCount = topicCategory.Topics?.Count ?? 0;
        CreatedAt = topicCategory.CreatedAt;
    }
}
