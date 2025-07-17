using App.Entities.Entities.App;

namespace App.Entities.DTOs.TopicCategories;

public class CreateTopicCategoryResDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public CreateTopicCategoryResDTO() { }

    public CreateTopicCategoryResDTO(TopicCategory topicCategory)
    {
        Id = topicCategory.Id;
        Name = topicCategory.Name;
        Description = topicCategory.Description;
        CreatedAt = topicCategory.CreatedAt;
        CreatedBy = topicCategory.CreatedBy;
    }
}
