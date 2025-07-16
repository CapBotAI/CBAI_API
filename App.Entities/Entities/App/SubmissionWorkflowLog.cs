using System;
using App.Entities.Entities.Core;

namespace App.Entities.Entities.App;

public partial class SubmissionWorkflowLog
{
    public int Id { get; set; }

    public int SubmissionId { get; set; }

    public int? FromStateId { get; set; }

    public int ToStateId { get; set; }

    public int ChangedBy { get; set; }

    public string? Reason { get; set; }

    public string? AdditionalData { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Submission Submission { get; set; } = null!;
    public virtual WorkflowState? FromState { get; set; }
    public virtual WorkflowState ToState { get; set; } = null!;
    public virtual User ChangedByUser { get; set; } = null!;
}
