using System;

namespace App.Entities.DTOs.PhaseTypes;

public class UpdatePhaseTypeResDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = null!;

    public UpdatePhaseTypeResDTO(App.Entities.Entities.App.PhaseType phaseType)
    {
        Id = phaseType.Id;
        Name = phaseType.Name;
        Description = phaseType.Description;
        UpdatedAt = phaseType.LastModifiedAt ?? DateTime.Now;
        UpdatedBy = phaseType.LastModifiedBy ?? string.Empty;
    }
}
