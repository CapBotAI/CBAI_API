using System;

namespace App.Entities.DTOs.PhaseTypes;

public class CreatePhaseTypeResDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public CreatePhaseTypeResDTO(App.Entities.Entities.App.PhaseType phaseType)
    {
        Id = phaseType.Id;
        Name = phaseType.Name;
        Description = phaseType.Description;
        CreatedBy = phaseType.CreatedBy;
        CreatedAt = phaseType.CreatedAt;
    }
}
