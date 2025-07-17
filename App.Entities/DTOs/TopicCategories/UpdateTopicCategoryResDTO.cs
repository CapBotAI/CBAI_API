using App.Entities.Entities.App;

namespace App.Entities.DTOs.TopicCategories;

public class UpdateTopicCategoryResDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    public UpdateTopicCategoryResDTO() { }

    public UpdateTopicCategoryResDTO(TopicCategory topicCategory)
    {
        Id = topicCategory.Id;
        Name = topicCategory.Name;
        Description = topicCategory.Description;
        LastModifiedAt = topicCategory.LastModifiedAt;
        LastModifiedBy = topicCategory.LastModifiedBy;
    }
}
