namespace App.Entities.DTOs.PhaseTypes;

public class PhaseTypeOverviewResDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public PhaseTypeOverviewResDTO(App.Entities.Entities.App.PhaseType phaseType)
    {
        Id = phaseType.Id;
        Name = phaseType.Name;
        Description = phaseType.Description;
    }
}
