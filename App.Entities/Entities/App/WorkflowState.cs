using System;
using System.Collections.Generic;

namespace App.Entities.Entities.App;

public partial class WorkflowState
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsFinalState { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<WorkflowTransition> FromTransitions { get; set; } = new List<WorkflowTransition>();
    public virtual ICollection<WorkflowTransition> ToTransitions { get; set; } = new List<WorkflowTransition>();
    public virtual ICollection<SubmissionWorkflowLog> FromStateLogs { get; set; } = new List<SubmissionWorkflowLog>();
    public virtual ICollection<SubmissionWorkflowLog> ToStateLogs { get; set; } = new List<SubmissionWorkflowLog>();
}
