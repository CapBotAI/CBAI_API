using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.Topics;

public class TopicOverviewResDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string SupervisorName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public int MaxStudents { get; set; }
    public bool IsApproved { get; set; }
    public bool IsLegacy { get; set; }
    public TopicStatus CurrentStatus { get; set; }
    public int CurrentVersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    public TopicOverviewResDTO() { }

    public TopicOverviewResDTO(Topic topic)
    {
        Id = topic.Id;
        Title = topic.Title;
        Description = topic.Description;
        SupervisorName = topic.Supervisor?.UserName ?? "";
        CategoryName = topic.Category?.Name ?? "";
        SemesterName = topic.Semester?.Name ?? "";
        MaxStudents = topic.MaxStudents;
        IsApproved = topic.IsApproved;
        IsLegacy = topic.IsLegacy;

        var latestVersion = topic.TopicVersions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        CurrentStatus = latestVersion?.Status ?? TopicStatus.Draft;
        CurrentVersionNumber = latestVersion?.VersionNumber ?? 1;
        CreatedAt = topic.CreatedAt;
    }
}
