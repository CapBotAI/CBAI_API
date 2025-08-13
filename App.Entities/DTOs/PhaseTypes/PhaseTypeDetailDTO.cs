using System;

namespace App.Entities.DTOs.PhaseTypes;

public class PhaseTypeDetailDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = null!;

    public PhaseTypeDetailDTO(App.Entities.Entities.App.PhaseType phaseType)
    {
        Id = phaseType.Id;
        Name = phaseType.Name;
        Description = phaseType.Description;
        CreatedAt = phaseType.CreatedAt;
        CreatedBy = phaseType.CreatedBy ?? string.Empty;
        UpdatedAt = phaseType.LastModifiedAt ?? DateTime.Now;
        UpdatedBy = phaseType.LastModifiedBy ?? string.Empty;
    }
}
