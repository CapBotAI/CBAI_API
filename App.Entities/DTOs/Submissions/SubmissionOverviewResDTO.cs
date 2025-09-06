using System;
using App.Entities.Entities.App;

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

    public SubmissionOverviewResDTO(Submission submission)
    {
        Id = submission.Id;
        TopicId = submission.TopicId;
        TopicTitle = submission.Topic.Title;
        SubmittedBy = submission.SubmittedBy;
        SubmittedByName = submission.SubmittedByUser.UserName;
        SubmissionRound = submission.SubmissionRound;
        SubmittedAt = submission.SubmittedAt;
    }
}
