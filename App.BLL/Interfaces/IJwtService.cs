using System;
using App.Entities.DTOs.Auth;
using App.Entities.Entities_2;

namespace App.BLL.Interfaces;

public interface IJwtService
{
    Task<JwtTokenDTO> GenerateTokenAsync(SystemAccount user);
    // Task<JwtTokenDTO> GenerateJwtToken(ViroCureUser user, bool isRemember, bool isAdmin, bool isManager = false, bool isStaff = false, bool isCustomer = false);
    Task<bool> ValidateTokenAsync(string token);
    string GetUserIdFromToken(string token);
}
