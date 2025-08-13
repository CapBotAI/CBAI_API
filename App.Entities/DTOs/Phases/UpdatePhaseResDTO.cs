using System;
using App.Entities.Entities.App;

namespace App.Entities.DTOs.Phases;

public class UpdatePhaseResDTO
{
    public int Id { get; set; }
    public int SemesterId { get; set; }
    public int PhaseTypeId { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? SubmissionDeadline { get; set; }

    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    public UpdatePhaseResDTO(Phase phase)
    {
        Id = phase.Id;
        SemesterId = phase.SemesterId;
        PhaseTypeId = phase.PhaseTypeId;
        Name = phase.Name;
        StartDate = phase.StartDate;
        EndDate = phase.EndDate;
        SubmissionDeadline = phase.SubmissionDeadline;
        LastModifiedAt = phase.LastModifiedAt;
        LastModifiedBy = phase.LastModifiedBy;
    }
}
