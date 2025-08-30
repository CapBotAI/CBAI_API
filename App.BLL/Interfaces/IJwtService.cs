using System;
using App.Entities.DTOs.Auth;
using App.Entities.Entities.Core;

namespace App.BLL.Interfaces;

public interface IJwtService
{
    Task<JwtTokenDTO> GenerateJwtToken(User user);
    Task<JwtTokenDTO> GenerateJwtTokenWithSpecificRole(User user, string role);
    Task<bool> ValidateTokenAsync(string token);
    string GetUserIdFromToken(string token);
}
