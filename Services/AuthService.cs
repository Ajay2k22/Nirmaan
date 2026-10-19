using Microsoft.EntityFrameworkCore;
using Nirmaan.Api.Data;
using Nirmaan.Api.Interface;
using Nirmaan.Api.Models;

namespace Nirmaan.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwtService;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;

    public AuthService(AppDbContext db, IJwtTokenService jwtService, ILogger<AuthService> logger, IEmailService emailService)
    {
        _db = db;
        _jwtService = jwtService;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<(bool success, int statusCode, string message, object? data)> RegisterAsync(RegisterDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return (false, 400, "Email and Password are required", null);

            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return (false, 409, "Email already exists", null);

            var user = new User
            {
                UserId = string.IsNullOrEmpty(dto.UserId) ? Guid.NewGuid().ToString() : dto.UserId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsCitizen = dto.IsCitizen,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = _jwtService.GenerateCitizenToken(user.UserId, user.Email);
            return (true, 200, "User registered successfully", new { token, userId = user.UserId, isCitizen = user.IsCitizen });
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "DB error in Register Email:{Email}", dto.Email);
            return (false, 500, "Database error while registering user", new { error = dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Register");
            return (false, 500, "Internal server error while registering", new { error = ex.Message });
        }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> LoginAsync(LoginDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return (false, 400, "Email and Password are required", null);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return (false, 404, "User not found with this email", null);

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                return (false, 401, "Invalid credentials", null);

            var token = _jwtService.GenerateCitizenToken(user.UserId, user.Email);
            return (true, 200, "Login successful", new { token, userId = user.UserId, isCitizen = user.IsCitizen });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Login Email:{Email}", dto.Email);
            return (false, 500, "Internal server error while logging in", new { error = ex.Message });
        }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> ForgetPasswordAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, 400, "Email is required", null);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return (false, 404, "User not found with this email", null);
            // Generate 6-digit unique code
            var uniqueCode = new Random().Next(100000, 999999).ToString();
            // Save to Code table (Entity from your diagram)
            var codeEntry = new Code
            {
                CodeId = Guid.NewGuid().ToString(),
                Email = email,
                UniqueCode = uniqueCode,
                Expiry = DateTime.UtcNow.AddMinutes(15)
            };
            // Remove old codes for this email
            var oldCodes = _db.Codes.Where(c => c.Email == email);
            _db.Codes.RemoveRange(oldCodes);
            _db.Codes.Add(codeEntry);
            await _db.SaveChangesAsync();
            // SEND MAIL INSTEAD OF RETURNING CODE
            try
            {
                await _emailService.SendResetCodeAsync(email, uniqueCode);
            }
            catch (Exception mailEx)
            {
                _logger.LogError(mailEx, "Failed to send email to {Email}", email);
                // Still don't expose code, but tell user email failed
                return (false, 500, "Failed to send email. Check SMTP config", new { error = mailEx.Message });
            }
            // New response - NO unique_code in response
            return (true, 200, $"Reset code sent to {email}", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ForgetPassword");
            return (false, 500, "Internal server error", new { error = ex.Message });
        }
    }
    public async Task<(bool success, int statusCode, string message, object? data)> ResetPasswordAsync(ResetPwdDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.UniqueCode) || string.IsNullOrWhiteSpace(dto.Pwd))
                return (false, 400, "Email, UniqueCode and Pwd are required", null);

            var code = await _db.Codes.FirstOrDefaultAsync(c =>
                c.Email == dto.Email &&
                c.UniqueCode == dto.UniqueCode &&
                !c.IsUsed &&
                c.Expiry > DateTime.UtcNow);

            if (code == null)
                return (false, 400, "Invalid or expired unique_code", null);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return (false, 404, "User not found", null);

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Pwd);
            code.IsUsed = true;
            await _db.SaveChangesAsync();

            var token = _jwtService.GenerateCitizenToken(user.UserId, user.Email);
            return (true, 200, "Password reset successful", new { token, userId = user.UserId, isCitizen = user.IsCitizen });
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "DB error in ResetPwd Email:{Email}", dto.Email);
            return (false, 500, "Database error while resetting password", new { error = dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResetPwd");
            return (false, 500, "Internal server error while resetting password", new { error = ex.Message });
        }
    }
}
