using System;
using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.Submissions;

public class SubmissionOverviewResDTO
{
    public int Id { get; set; }

    public int TopicId { get; set; }

    public string? TopicTitle { get; set; }

    public int SubmittedBy { get; set; }
    public string? SubmittedByName { get; set; }

    public int SubmissionRound { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public SubmissionStatus Status { get; set; }

    public SubmissionOverviewResDTO(Submission submission)
    {
        Id = submission.Id;
        TopicId = submission.TopicId;
        TopicTitle = submission.Topic.EN_Title;
        SubmittedBy = submission.SubmittedBy;
        SubmittedByName = submission.SubmittedByUser.UserName;
        SubmissionRound = submission.SubmissionRound;
        SubmittedAt = submission.SubmittedAt;
        Status = submission.Status;
    }
}