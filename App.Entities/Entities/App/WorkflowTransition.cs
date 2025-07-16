using System;
using App.Entities.Entities.Core;

namespace App.Entities.Entities.App;

public partial class WorkflowTransition
{
    public int Id { get; set; }

    public int? FromStateId { get; set; }

    public int ToStateId { get; set; }

    public int? RequiredRoleId { get; set; }

    public string? ConditionRules { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual WorkflowState? FromState { get; set; }
    public virtual WorkflowState ToState { get; set; } = null!;
    public virtual Role? RequiredRole { get; set; }
}
