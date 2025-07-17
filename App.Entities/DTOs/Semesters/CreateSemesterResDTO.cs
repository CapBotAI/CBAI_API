using System;

namespace App.Entities.DTOs.Semesters;

public class CreateSemesterResDTO
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public CreateSemesterResDTO(App.Entities.Entities.App.Semester semester)
    {
        Id = semester.Id;
        Name = semester.Name;
        StartDate = semester.StartDate;
        EndDate = semester.EndDate;
        CreatedBy = semester.CreatedBy;
        CreatedAt = semester.CreatedAt;
    }
}
