using System;

namespace App.Entities.DTOs.Semesters;

public class UpdateSemesterResDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = null!;

    public UpdateSemesterResDTO(App.Entities.Entities.App.Semester semester)
    {
        Id = semester.Id;
        Name = semester.Name;
        StartDate = semester.StartDate;
        EndDate = semester.EndDate;
        UpdatedAt = semester.LastModifiedAt;
        UpdatedBy = semester.LastModifiedBy ?? string.Empty;
    }
}
