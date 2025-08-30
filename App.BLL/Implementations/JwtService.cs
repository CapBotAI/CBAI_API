using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using App.BLL.Interfaces;
using App.Commons;
using App.DAL.Interfaces;
using App.Entities.Constants;
using App.Entities.DTOs.Accounts;
using App.Entities.DTOs.Auth;
using App.Entities.Entities.Core;
using App.Entities.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace App.BLL.Implementations;

public class JwtService : IJwtService
{
    private readonly ILogger<JwtService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IIdentityRepository _identityRepository;


    public JwtService(
        ILogger<JwtService> logger,
        IConfiguration configuration,
        IIdentityRepository identityRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _identityRepository = identityRepository;

    }


    public async Task<JwtTokenDTO> GenerateJwtToken(User user)
    {
        var email = user.Email ?? string.Empty;
        var userId = user.Id.ToString();
        var userName = user.UserName ?? string.Empty;

        var claims = new List<Claim>
    {
        new(ClaimTypes.Email, email),
        new(ClaimTypes.NameIdentifier, userId),
        new(ClaimTypes.Name, userName),
        new(ConstantModel.CLAIM_EMAIL, email),
        new(ConstantModel.POLICY_VERIFY_EMAIL, user.EmailConfirmed.ToString()),
        new(ConstantModel.CLAIM_ID, userId),
    };

        var roles = await _identityRepository.GetRolesAsync(user.Id);
        claims.AddRange(GetRoleClaims(roles));

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = ConstantModel.JWT_ISSUER,
            Audience = ConstantModel.JWT_AUDIENCE
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new JwtTokenDTO
        {
            AccessToken = tokenHandler.WriteToken(token),
            RefreshToken = "Tạm thời chưa implement",
            ExpiryTime = tokenDescriptor.Expires.Value
        };
    }


    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = ConstantModel.JWT_ISSUER,
                ValidateAudience = true,
                ValidAudience = ConstantModel.JWT_AUDIENCE,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetUserIdFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.ReadJwtToken(token);
        return jwt.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    #region Private Methods

    private static IEnumerable<Claim> GetRoleClaims(IEnumerable<string> roles)
    {
        var claims = new List<Claim>();

        if (roles.Contains(SystemRoleConstants.Administrator))
            claims.Add(new Claim(ConstantModel.IS_ADMIN, "true"));

        if (roles.Contains(SystemRoleConstants.Moderator))
            claims.Add(new Claim(ConstantModel.IS_MODERATOR, "true"));

        if (roles.Contains(SystemRoleConstants.Supervisor))
            claims.Add(new Claim(ConstantModel.IS_SUPERVISOR, "true"));

        if (roles.Contains(SystemRoleConstants.Reviewer))
            claims.Add(new Claim(ConstantModel.IS_REVIEWER, "true"));

        return claims;
    }

    #endregion
}
