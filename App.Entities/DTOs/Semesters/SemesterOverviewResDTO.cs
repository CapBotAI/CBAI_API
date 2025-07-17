using System;

namespace App.Entities.DTOs.Semesters;

public class SemesterOverviewResDTO
{
    public int Id { get; set; }
    public string? Name { get; set; }

    public SemesterOverviewResDTO(App.Entities.Entities.App.Semester semester)
    {
        Id = semester.Id;
        Name = semester.Name;
    }
}
