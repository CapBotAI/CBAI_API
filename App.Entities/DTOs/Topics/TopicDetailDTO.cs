using App.Entities.DTOs.TopicVersions;
using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.Topics;

public class TopicDetailDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Objectives { get; set; }
    public int SupervisorId { get; set; }
    public string SupervisorName { get; set; } = null!;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = null!;
    public int MaxStudents { get; set; }
    public bool IsApproved { get; set; }
    public bool IsLegacy { get; set; }
    public TopicStatus CurrentStatus { get; set; }
    public int TotalVersions { get; set; }
    public TopicVersionDetailDTO? CurrentVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    public TopicDetailDTO() { }

    public TopicDetailDTO(Topic topic)
    {
        Id = topic.Id;
        Title = topic.Title;
        Description = topic.Description;
        Objectives = topic.Objectives;
        SupervisorId = topic.SupervisorId;
        SupervisorName = topic.Supervisor?.UserName ?? "";
        CategoryId = topic.CategoryId ?? 0;
        CategoryName = topic.Category?.Name ?? "";
        SemesterId = topic.SemesterId;
        SemesterName = topic.Semester?.Name ?? "";
        MaxStudents = topic.MaxStudents;
        IsApproved = topic.IsApproved;
        IsLegacy = topic.IsLegacy;
        TotalVersions = topic.TopicVersions?.Count ?? 0;

        var latestVersion = topic.TopicVersions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        CurrentStatus = latestVersion?.Status ?? TopicStatus.Draft;
        CurrentVersion = latestVersion != null ? new TopicVersionDetailDTO(latestVersion) : null;

        CreatedAt = topic.CreatedAt;
        CreatedBy = topic.CreatedBy;
        LastModifiedAt = topic.LastModifiedAt;
        LastModifiedBy = topic.LastModifiedBy;
    }
}
