using System;

namespace App.Entities.DTOs.Auth;

public class LoginResponseDTO
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public JwtTokenDTO? TokenData { get; set; }
}

public class LoginResponseDTOV2
{
    public string Token { get; set; } = null!;
    public string Role { get; set; } = null!;
}
