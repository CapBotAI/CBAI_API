using System;
using App.Entities.Entities.App;

namespace App.Entities.DTOs.Phases;

public class PhaseOverviewResDTO
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? PhaseTypeName { get; set; }
    public string? SemesterName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? SubmissionDeadline { get; set; }

    public PhaseOverviewResDTO(Phase phase)
    {
        Id = phase.Id;
        Name = phase.Name;
        PhaseTypeName = phase.PhaseType.Name;
        SemesterName = phase.Semester.Name;
        StartDate = phase.StartDate;
        EndDate = phase.EndDate;
        SubmissionDeadline = phase.SubmissionDeadline;
    }
}
