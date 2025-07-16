using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using App.BLL.Interfaces;
using App.Commons;
using App.Entities.DTOs.Accounts;
using App.Entities.DTOs.Auth;
using App.Entities.Entities_2;
using App.Entities.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace App.BLL.Implementations;

public class JwtService : IJwtService
{
    private readonly ILogger<JwtService> _logger;
    private readonly IConfiguration _configuration;

    public JwtService(
        ILogger<JwtService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<JwtTokenDTO> GenerateTokenAsync(SystemAccount user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]);

        var roleName = user.Role switch
        {
            1 => nameof(UserRoles.Administrator),
            2 => nameof(UserRoles.Moderator),
            3 => nameof(UserRoles.Developer),
            4 => nameof(UserRoles.Member),
            _ => "Other"
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.AccountID.ToString()),
            new(ClaimTypes.Email, user.Email ?? ""),
            new("jti", Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, roleName)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = ConstantModel.JWT_ISSUER,
            Audience = ConstantModel.JWT_AUDIENCE
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        var userData = new UserViewDTO(user);

        return new JwtTokenDTO
        {
            AccessToken = accessToken,
            User = userData,
            ExpiryTime = tokenDescriptor.Expires.Value
        };
    }


    // public async Task<JwtTokenDTO> GenerateJwtToken(ViroCureUser user, bool isRemember, bool isAdmin, bool isManager = false, bool isStaff = false, bool isAuthor = false)
    // {
    //     var claims = new List<Claim> {
    //             new (ClaimTypes.Email, user.Email ?? string.Empty),
    //             new (ClaimTypes.NameIdentifier, user.Id.ToString()),
    //             new (ClaimTypes.Name, user.UserName ?? string.Empty),
    //             new (ConstantModel.CLAIM_EMAIL, user.Email ?? string.Empty),
    //             new (ConstantModel.POLICY_VERIFY_EMAIL, user.EmailConfirmed.ToString()),
    //             new (ConstantModel.CLAIM_ID, user.Id.ToString()),
    //             new (ConstantModel.IS_ADMIN, isAdmin.ToString()),
    //             new (ConstantModel.IS_MANAGER, isManager.ToString()),
    //             new (ConstantModel.IS_STAFF, isStaff.ToString()),
    //             new (ConstantModel.IS_AUTHOR, isAuthor.ToString()),
    //         };
    //
    //     var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]);
    //
    //     var tokenDescriptor = new SecurityTokenDescriptor
    //     {
    //         Subject = new ClaimsIdentity(claims),
    //         Expires = isRemember ? DateTime.Now.AddDays(28) : DateTime.Now.AddHours(24),
    //         SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
    //         Issuer = ConstantModel.JWT_ISSUER,
    //         Audience = ConstantModel.JWT_AUDIENCE
    //     };
    //
    //     var tokenHandler = new JwtSecurityTokenHandler();
    //     var token = tokenHandler.CreateToken(tokenDescriptor);
    //     var accessToken = tokenHandler.WriteToken(token);
    //     var refreshToken = GenerateRefreshToken();
    //
    //     return new JwtTokenDTO
    //     {
    //         AccessToken = accessToken,
    //         RefreshToken = refreshToken,
    //         ExpiryTime = tokenDescriptor.Expires.Value
    //     };
    // }


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
}
