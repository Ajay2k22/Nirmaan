using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; 
using Nirmaan.Api.Interface;

namespace Nirmaan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message, error = result.data });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, token = data.token, userId = data.userId, isCitizen = data.isCitizen });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in Register");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error while registering", error = ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, token = data.token, userId = data.userId, isCitizen = data.isCitizen });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in Login");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error while logging in", error = ex.Message });
        }
    }

    [HttpPost("forget-pwd")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgetPwd([FromBody] ForgetPwdDto dto)
    {
        try
        {
            // FIX 1: Proper validation - should be 400 not 500
            if (string.IsNullOrWhiteSpace(dto?.Email))
            {
                // Raise exception that will be caught by GlobalExceptionMiddleware
                throw new ArgumentException("Email is required");
                // OR if you don't want exception, use:
                // return BadRequest(new { statusCode = 400, message = "Email is required" });
            }

            // FIX 2: No toString() needed - Email is already string
            var result = await _authService.ForgetPasswordAsync(dto.Email);

            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

            // FIX 3: New flow - DON'T return unique_code, email is sent
            // Old: var data = (dynamic)result.data!; return Ok(... unique_code = data.unique_code)
            // New:
            return Ok(new
            {
                statusCode = result.statusCode,
                message = result.message // "Reset code sent to xyz@gmail.com"
            });
        }
        catch (ArgumentException argEx)
        {
            // Validation exception
            _logger.LogWarning(argEx, "Validation failed in ForgetPwd");
            return BadRequest(new { statusCode = 400, message = argEx.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in ForgetPwd");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost("reset-pwd")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPwd([FromBody] ResetPwdDto dto)
    {
        try
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, token = data.token, userId = data.userId, isCitizen = data.isCitizen });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in ResetPwd");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error while resetting password", error = ex.Message });
        }
    }
}
