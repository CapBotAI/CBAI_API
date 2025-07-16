using System;
using App.Entities.Entities_2;

namespace App.Entities.DTOs.Handbag;

public class HandbagResponseDTO
{

    public int HandbagID { get; set; }
    public string ModelName { get; set; } = null!;
    public string? Material { get; set; }
    public string? Color { get; set; }
    public decimal? Price { get; set; }
    public int? Stock { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public BrandInfoDTO? Brand { get; set; }

    public HandbagResponseDTO(App.Entities.Entities_2.Handbag handbag)
    {
        HandbagID = handbag.HandbagID;
        ModelName = handbag.ModelName;
        Material = handbag.Material;
        Color = handbag.Color;
        Price = handbag.Price;
        Stock = handbag.Stock;
        ReleaseDate = handbag.ReleaseDate;
        Brand = handbag.Brand != null ? new BrandInfoDTO(handbag.Brand) : null;
    }
}


public class BrandInfoDTO
{
    public int BrandID { get; set; }
    public string BrandName { get; set; } = null!;
    public string? Country { get; set; }
    public int? FoundedYear { get; set; }
    public string? Website { get; set; }

    public BrandInfoDTO(Brand brand)
    {
        BrandID = brand.BrandID;
        BrandName = brand.BrandName;
        Country = brand.Country;
        FoundedYear = brand.FoundedYear;
        Website = brand.Website;
    }
}
