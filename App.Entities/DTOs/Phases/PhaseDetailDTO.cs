using System;
using App.Entities.Entities.App;

namespace App.Entities.DTOs.Phases;

public class PhaseDetailDTO
{
    public int Id { get; set; }

    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }

    public int PhaseTypeId { get; set; }
    public string? PhaseTypeName { get; set; }

    public string Name { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? SubmissionDeadline { get; set; }

    public PhaseDetailDTO(Phase phase)
    {
        Id = phase.Id;
        Name = phase.Name;
        SemesterId = phase.SemesterId;
        PhaseTypeId = phase.PhaseTypeId;
        PhaseTypeName = phase.PhaseType.Name;
        SemesterName = phase.Semester.Name;
        StartDate = phase.StartDate;
        EndDate = phase.EndDate;
        SubmissionDeadline = phase.SubmissionDeadline;
    }
}
