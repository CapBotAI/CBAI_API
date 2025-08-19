using App.Entities.Entities.App;

namespace App.Entities.DTOs.Topics;

public class CreateTopicResDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string SupervisorName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public int MaxStudents { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public int CurrentVersionNumber { get; set; }

    public CreateTopicResDTO() { }

    public CreateTopicResDTO(Topic topic)
    {
        Id = topic.Id;
        Title = topic.Title;
        Description = topic.Description;
        SupervisorName = topic.Supervisor?.UserName ?? "";
        CategoryName = topic.Category?.Name ?? "";
        SemesterName = topic.Semester?.Name ?? "";
        MaxStudents = topic.MaxStudents;
        IsApproved = topic.IsApproved;
        CreatedAt = topic.CreatedAt;
        CreatedBy = topic.CreatedBy;
        CurrentVersionNumber = (topic.TopicVersions != null && topic.TopicVersions.Any())
            ? topic.TopicVersions.Max(v => v.VersionNumber)
            : 0;
    }
}
