using System;
using App.Entities.Entities.App;

namespace App.Entities.DTOs.Topics;

public class UpdateTopicResDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string SupervisorName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public int MaxStudents { get; set; }
    public bool IsApproved { get; set; }
    public long? FileId { get; set; }
    public string? DocumentUrl { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public int CurrentVersionNumber { get; set; }

    public UpdateTopicResDTO(Topic topic, EntityFile? entityFile)
    {
        Id = topic.Id;
        Title = topic.Title;
        Description = topic.Description;
        SupervisorName = topic.Supervisor?.UserName ?? "";
        CategoryName = topic.Category?.Name ?? "";
        SemesterName = topic.Semester?.Name ?? "";
        MaxStudents = topic.MaxStudents;
        IsApproved = topic.IsApproved;
        UpdatedAt = topic.LastModifiedAt ?? DateTime.Now;
        UpdatedBy = topic.LastModifiedBy;
        CurrentVersionNumber = topic.TopicVersions.Any()
            ? topic.TopicVersions.Max(x => x.VersionNumber)
            : 0;

        FileId = entityFile?.FileId;
        DocumentUrl = entityFile?.File?.Url;
    }
}
