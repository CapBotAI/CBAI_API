using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace App.Entities.ValidationAttributes;

public class FullNameValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return false;

        string fullName = value.ToString()!;

        if (!Regex.IsMatch(fullName, @"^[a-zA-Z0-9@#.\s]+$"))
        {
            ErrorMessage = "Fullname can only contain letters, numbers, spaces, @, #, and dot";
            return false;
        }

        var words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (!char.IsUpper(word[0]))
            {
                ErrorMessage = "Each word of the Fullname must begin with the capital letter";
                return false;
            }
        }

        return true;
    }
}

public class BirthdayValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return false;

        if (value is DateOnly birthday)
        {
            var minDate = new DateOnly(2007, 1, 1);
            if (birthday >= minDate)
            {
                ErrorMessage = "Value for Birthday < 01-01-2007";
                return false;
            }
            return true;
        }

        return false;
    }
}

public class PhoneNumberValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return false;

        string phoneNumber = value.ToString()!;

        if (!Regex.IsMatch(phoneNumber, @"^\+84\d{9,10}$"))
        {
            ErrorMessage = "Phone number must be in the format +84989xxxxxx";
            return false;
        }

        return true;
    }
}

public class ResistanceRateValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return false;

        if (value is double rate)
        {
            if (rate < 0 || rate > 1)
            {
                ErrorMessage = "Resistance Rate: Must be between 0 and 1";
                return false;
            }
            return true;
        }

        return false;
    }
}
