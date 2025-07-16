using System;
using System.ComponentModel.DataAnnotations;

namespace App.Entities.DTOs.Handbag;

public class UpdateHandbagDTO
{
    [ModelNameValidation]
    public string? ModelName { get; set; }

    public string? Material { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal? Price { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Stock must be greater than 0")]
    public int? Stock { get; set; }

    public int? BrandId { get; set; }
    public string? Color { get; set; }
    public DateOnly? ReleaseDate { get; set; }
}
