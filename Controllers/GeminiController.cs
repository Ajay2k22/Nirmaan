using Microsoft.AspNetCore.Mvc;
using Nirmaan.Api.Interface;
using Microsoft.AspNetCore.Authorization;
using Nirmaan.Api.Services;

namespace Nirmaan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeminiController : ControllerBase
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<GeminiController> _logger;

    public GeminiController(IGeminiService geminiService, ILogger<GeminiController> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    // NEW: Accepts file upload from Postman form-data (photo as File)
    [HttpPost("detectImage")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = "IsCitizen")]
    public async Task<IActionResult> DetectImage([FromForm] IFormFile photo)
    {
        try
        {
            if (photo == null || photo.Length == 0)
            {
                return BadRequest(new { statusCode = 400, message = "Photo file is required" });
            }

            // Convert IFormFile to base64 for Gemini service
            using var ms = new MemoryStream();
            await photo.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var base64 = Convert.ToBase64String(bytes);

            var dto = new GeminiDetectDto { Photo = base64 };
            var result = await _geminiService.DetectImageAsync(dto);

            if (!result.success)
            {
                return StatusCode(result.statusCode, new
                {
                    statusCode = result.statusCode,
                    message = result.message,
                    error = result.data
                });
            }

            var data = (dynamic)result.data!;
            return Ok(new
            {
                statusCode = result.statusCode,
                message = result.message,
                severity = data.severity,
                reason = data.reason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in DetectImage form-data");
            return StatusCode(500, new
            {
                statusCode = 500,
                message = "Internal server error while detecting image",
                error = ex.Message,
                severity = "medium",
                reason = "Issue detected - requires manual verification"
            });
        }
    }

    // OLD: Still supports JSON base64 for frontend Angular
    [HttpPost("detectImageJson")]
    public async Task<IActionResult> DetectImageJson([FromBody] GeminiDetectDto dto)
    {
        try
        {
            var result = await _geminiService.DetectImageAsync(dto);

            if (!result.success)
            {
                return StatusCode(result.statusCode, new
                {
                    statusCode = result.statusCode,
                    message = result.message,
                    error = result.data
                });
            }

            var data = (dynamic)result.data!;
            return Ok(new
            {
                statusCode = result.statusCode,
                message = result.message,
                severity = data.severity,
                reason = data.reason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in DetectImageJson");
            return StatusCode(500, new
            {
                statusCode = 500,
                message = "Internal server error while detecting image",
                error = ex.Message,
                severity = "medium",
                reason = "Issue detected - requires manual verification"
            });
        }
    }
}
