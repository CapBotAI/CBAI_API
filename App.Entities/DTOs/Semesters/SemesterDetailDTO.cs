using System;

namespace App.Entities.DTOs.Semesters;

public class SemesterDetailDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = null!;
    public string? Description { get; set; }

    public SemesterDetailDTO(App.Entities.Entities.App.Semester semester)
    {
        Id = semester.Id;
        Name = semester.Name;
        StartDate = semester.StartDate;
        EndDate = semester.EndDate;
        CreatedAt = semester.CreatedAt;
        CreatedBy = semester.CreatedBy ?? string.Empty;
        UpdatedAt = semester.LastModifiedAt ?? DateTime.Now;
        UpdatedBy = semester.LastModifiedBy ?? string.Empty;
        Description = semester.Description;
    }
}
