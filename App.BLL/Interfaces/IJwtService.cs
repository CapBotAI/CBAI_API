using System;
using App.Entities.DTOs.Auth;
using App.Entities.Entities.Core;

namespace App.BLL.Interfaces;

public interface IJwtService
{
    Task<JwtTokenDTO> GenerateJwtToken(User user);
    Task<bool> ValidateTokenAsync(string token);
    string GetUserIdFromToken(string token);
}
