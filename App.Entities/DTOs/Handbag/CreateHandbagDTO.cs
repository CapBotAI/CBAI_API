using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace App.Entities.DTOs.Handbag;

public class CreateHandbagDTO
{
    [Required(ErrorMessage = "ModelName is required")]
    [ModelNameValidation]
    public string ModelName { get; set; } = null!;

    [Required(ErrorMessage = "Material is required")]
    public string Material { get; set; } = null!;

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Stock is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Stock must be greater than 0")]
    public int Stock { get; set; }

    [Required(ErrorMessage = "BrandId is required")]
    public int BrandId { get; set; }

    public string? Color { get; set; }
    public DateOnly? ReleaseDate { get; set; }
}

public class ModelNameValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return false;

        string modelName = value.ToString()!;

        // Correct regex pattern
        string pattern = @"^([A-Z0-9][a-zA-Z0-9#]*\s)*([A-Z0-9][a-zA-Z0-9#]*)$";

        if (!Regex.IsMatch(modelName, pattern))
        {
            ErrorMessage = "ModelName must start with an uppercase letter or digit, contain only alphanumerics or '#', and be space-separated if multiple words.";
            return false;
        }

        return true;
    }
}

