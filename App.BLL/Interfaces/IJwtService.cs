using System;
using App.Entities.DTOs.Auth;
using App.Entities.Entities.Core;

namespace App.BLL.Interfaces;

public interface IJwtService
{
    Task<JwtTokenDTO> GenerateTokenAsync(User user);
    // Task<JwtTokenDTO> GenerateJwtToken(ViroCureUser user, bool isRemember, bool isAdmin, bool isManager = false, bool isStaff = false, bool isCustomer = false);
    Task<bool> ValidateTokenAsync(string token);
    string GetUserIdFromToken(string token);
}
