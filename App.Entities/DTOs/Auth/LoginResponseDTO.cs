using System;

namespace App.Entities.DTOs.Auth;

public class LoginResponseDTO
{
    public JwtTokenDTO? TokenData { get; set; }
}
