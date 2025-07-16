using System;

namespace App.Entities.DTOs.Handbag;

public class SearchHandbagResultDTO
{
    public string BrandName { get; set; } = null!;
    public List<HandbagResponseDTO> Handbags { get; set; } = new List<HandbagResponseDTO>();
}
