using System;
using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.Submissions;

public class SubmissionDetailDTO
{
    public int Id { get; set; }

    public int TopicVersionId { get; set; }
    public string? TopicVersionTitle { get; set; }

    public int PhaseId { get; set; }
    public string? PhaseName { get; set; }

    public int SubmittedBy { get; set; }
    public string? SubmittedByName { get; set; }

    public int SubmissionRound { get; set; } = 1;

    public string? DocumentUrl { get; set; }

    public string? AdditionalNotes { get; set; }

    public AiCheckStatus AiCheckStatus { get; set; } = AiCheckStatus.Pending;

    public decimal? AiCheckScore { get; set; }

    public string? AiCheckDetails { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    public DateTime? SubmittedAt { get; set; }

    public SubmissionDetailDTO(Submission submission)
    {
        Id = submission.Id;
        TopicVersionId = submission.TopicVersionId;
        PhaseId = submission.PhaseId;
        SubmittedBy = submission.SubmittedBy;
        SubmissionRound = submission.SubmissionRound;
        DocumentUrl = submission.DocumentUrl;
        AdditionalNotes = submission.AdditionalNotes;
        AiCheckStatus = submission.AiCheckStatus;
        AiCheckScore = submission.AiCheckScore;
        AiCheckDetails = submission.AiCheckDetails;
        Status = submission.Status;
        SubmittedAt = submission.SubmittedAt;
    }
}
