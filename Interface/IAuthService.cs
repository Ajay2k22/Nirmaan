

namespace Nirmaan.Api.Interface;

public class RegisterDto
{
    public string? UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool IsCitizen { get; set; } = true;
}

public record LoginDto(string Email, string Password);
public record ForgetPwdDto(string Email);
public record ResetPwdDto(string UniqueCode, string Email, string Pwd);

public interface IAuthService
{
    Task<(bool success, int statusCode, string message, object? data)> RegisterAsync(RegisterDto dto);
    Task<(bool success, int statusCode, string message, object? data)> LoginAsync(LoginDto dto);
    // Task<(bool success, int statusCode, string message, object? data)> ForgetPasswordAsync(ForgetPwdDto dto);
    Task<(bool success, int statusCode, string message, object? data)> ResetPasswordAsync(ResetPwdDto dto);
    Task<(bool success, int statusCode, string message, object? data)> ForgetPasswordAsync(string email);
}
