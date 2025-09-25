using System;

namespace App.Entities.DTOs.Semesters;

public class SemesterOverviewResDTO
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTime starDate { get; set; }
    public DateTime endDate { get; set; }
    public string? Description { get; set; }


    public SemesterOverviewResDTO(App.Entities.Entities.App.Semester semester)
    {
        Id = semester.Id;
        Name = semester.Name;
        starDate = semester.StartDate;
        endDate = semester.EndDate;
        Description = semester.Description;
        
    }
}
