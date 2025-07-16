using System;
using System.ComponentModel.DataAnnotations;

namespace App.Entities.DTOs.Handbag;

public class HandbagODataDTO
{
    [Key]
    public int HandbagID { get; set; }
    public string ModelName { get; set; } = null!;
    public string? Material { get; set; }
    public string? Color { get; set; }
    public decimal? Price { get; set; }
    public int? Stock { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public int? BrandID { get; set; }

    // Navigation property for OData expand
    public virtual BrandODataDTO? Brand { get; set; }
}

public class BrandODataDTO
{
    [Key]
    public int BrandID { get; set; }
    public string BrandName { get; set; } = null!;
    public string? Country { get; set; }
    public int? FoundedYear { get; set; }
    public string? Website { get; set; }
}
