using System;
using System.Collections.Generic;
using App.Entities.Entities.Core;
using App.Entities.Enums;

namespace App.Entities.Entities.App;

public partial class Submission
{
    public int Id { get; set; }

    public int TopicVersionId { get; set; }

    public int PhaseId { get; set; }

    public int SubmittedBy { get; set; }

    public int SubmissionRound { get; set; } = 1;

    public string? DocumentUrl { get; set; }

    public string? AdditionalNotes { get; set; }

    public AiCheckStatus AiCheckStatus { get; set; } = AiCheckStatus.Pending;

    public decimal? AiCheckScore { get; set; }

    public string? AiCheckDetails { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    public DateTime? SubmittedAt { get; set; }

    public virtual TopicVersion TopicVersion { get; set; } = null!;
    public virtual Phase Phase { get; set; } = null!;
    public virtual User SubmittedByUser { get; set; } = null!;
    public virtual ICollection<ReviewerAssignment> ReviewerAssignments { get; set; } = new List<ReviewerAssignment>();
    public virtual ICollection<SubmissionWorkflowLog> SubmissionWorkflowLogs { get; set; } = new List<SubmissionWorkflowLog>();
}
