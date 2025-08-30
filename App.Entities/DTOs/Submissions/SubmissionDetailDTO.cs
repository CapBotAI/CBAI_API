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

    public string? AdditionalNotes { get; set; }

    public AiCheckStatus AiCheckStatus { get; set; } = AiCheckStatus.Pending;

    public decimal? AiCheckScore { get; set; }

    public string? AiCheckDetails { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    public DateTime? SubmittedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public long? FileId { get; set; }
    public string? DocumentUrl { get; set; }

    public SubmissionDetailDTO(Submission submission, EntityFile? entityFile)
    {
        Id = submission.Id;
        TopicVersionId = submission.TopicVersionId ?? 0;
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
        CreatedAt = submission.CreatedAt;
        CreatedBy = submission.CreatedBy;
        LastModifiedAt = submission.LastModifiedAt;
        LastModifiedBy = submission.LastModifiedBy;
        DeletedAt = submission.DeletedAt;
        FileId = entityFile?.FileId;
        DocumentUrl = entityFile?.File?.Url;
    }
}
