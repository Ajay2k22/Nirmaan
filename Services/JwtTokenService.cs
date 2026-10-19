using Microsoft.IdentityModel.Tokens;
using Nirmaan.Api.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nirmaan.Api.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration config, ILogger<JwtTokenService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public string GenerateCitizenToken(string userId, string email)
    {
        try
        {
            var claims = new[]
            {
                new Claim("userId", userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim("role", "citizen"),
                new Claim("isCitizen", "true"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            return GenerateToken(claims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateCitizenToken UserId:{UserId}", userId);
            throw;
        }
    }

    public string GenerateAdminToken(string masterDataId, string mnc, string dept)
    {
        try
        {
            var claims = new[]
            {
                new Claim("userId", masterDataId),
                new Claim(ClaimTypes.NameIdentifier, masterDataId),
                new Claim("mnc_code", mnc),
                new Claim("dept_code", dept),
                new Claim("role", "admin"),
                new Claim("isCitizen", "false"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            return GenerateToken(claims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateAdminToken MNC:{MNC} DEPT:{DEPT}", mnc, dept);
            throw;
        }
    }

    public string GenerateCitizenToken(string userId, bool isCitizen)
        => GenerateCitizenToken(userId, $"{userId}@nirmaan.local");

    public string GenerateGovtBodyToken(bool isCitizen, string mnc_code, string dept_code, string? masterDataId = null)
        => GenerateAdminToken(masterDataId ?? $"{mnc_code}_{dept_code}", mnc_code, dept_code);

    private string GenerateToken(Claim[] claims)
    {
        try
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured in appsettings.json");
            var issuer = _config["Jwt:Issuer"] ?? "Nirmaan.Api";
            var audience = _config["Jwt:Audience"] ?? "Nirmaan.Client";
            var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "1440");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token");
            throw;
        }
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwtKey = _config["Jwt:Key"];
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtKey)) return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = "role",
                NameClaimType = "userId"
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid JWT token");
            return null;
        }
    }
}