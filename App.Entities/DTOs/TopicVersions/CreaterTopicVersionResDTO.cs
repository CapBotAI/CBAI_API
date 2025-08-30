using System;
using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.TopicVersions;

public class CreaterTopicVersionResDTO
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Objectives { get; set; }
    public string? Methodology { get; set; }
    public string? ExpectedOutcomes { get; set; }
    public string? Requirements { get; set; }

    public long? FileId { get; set; }
    public string? DocumentUrl { get; set; }
    public TopicStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public CreaterTopicVersionResDTO(TopicVersion topicVersion, EntityFile? entityFile)
    {
        Id = topicVersion.Id;
        TopicId = topicVersion.TopicId;
        VersionNumber = topicVersion.VersionNumber;
        Title = topicVersion.Title;
        Description = topicVersion.Description;
        Objectives = topicVersion.Objectives;
        Methodology = topicVersion.Methodology;
        ExpectedOutcomes = topicVersion.ExpectedOutcomes;
        Requirements = topicVersion.Requirements;
        Status = topicVersion.Status;
        CreatedAt = topicVersion.CreatedAt;
        CreatedBy = topicVersion.CreatedBy;

        FileId = entityFile?.FileId;
        DocumentUrl = entityFile?.File?.Url;
    }
}
