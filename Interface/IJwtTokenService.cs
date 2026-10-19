using System.Security.Claims;

namespace Nirmaan.Api.Interface;

public interface IJwtTokenService
{
    string GenerateCitizenToken(string userId, string email);
    string GenerateAdminToken(string masterDataId, string mnc, string dept);
    ClaimsPrincipal? ValidateToken(string token);
}
