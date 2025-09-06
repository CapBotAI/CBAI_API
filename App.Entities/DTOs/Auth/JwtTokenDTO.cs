using System;
using App.Entities.DTOs.Accounts;

namespace App.Entities.DTOs.Auth;

public class JwtTokenDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiryTime { get; set; }
    public UserOverviewDTO User { get; set; }
}
