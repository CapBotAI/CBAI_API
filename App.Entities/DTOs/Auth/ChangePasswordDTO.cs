using System.ComponentModel.DataAnnotations;

public class ChangePasswordDTO
{
    [Required(ErrorMessage = "Old password is required.")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
    public string NewPassword { get; set; } = string.Empty;
}