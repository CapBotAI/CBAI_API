using System;
using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.TopicVersions;

public class TopicVersionOverviewDTO
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public int VersionNumber { get; set; }
    public string EN_Title { get; set; } = null!;
    public string? VN_title { get; set; }
    public string? DocumentUrl { get; set; }
    public TopicStatus Status { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? PotentialDuplicate { get; set; }
    public TopicVersionOverviewDTO(TopicVersion topicVersion)
    {
        Id = topicVersion.Id;
        TopicId = topicVersion.TopicId;
        VersionNumber = topicVersion.VersionNumber;
        EN_Title = topicVersion.EN_Title;
        VN_title = topicVersion.VN_title;
        DocumentUrl = topicVersion.DocumentUrl;
        Status = topicVersion.Status;
        SubmittedAt = topicVersion.SubmittedAt;
        SubmittedByUserName = topicVersion.SubmittedByUser?.UserName;
        CreatedAt = topicVersion.CreatedAt;
        CreatedBy = topicVersion.CreatedBy;
        PotentialDuplicate = topicVersion.PotentialDuplicate;
    }
}