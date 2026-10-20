using Microsoft.AspNetCore.Mvc;
using Nirmaan.Api.Interface;
using Microsoft.AspNetCore.Authorization;
using Nirmaan.Api.Services;
using Nirmaan.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Nirmaan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeminiController : ControllerBase
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<GeminiController> _logger;
    private readonly AppDbContext _db;

    public GeminiController(IGeminiService geminiService, AppDbContext db, ILogger<GeminiController> logger)
    {
        _geminiService = geminiService;
        _db = db;
        _logger = logger;
    }

    [HttpPost("detectImage")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = "IsCitizen")]
    public async Task<IActionResult> DetectImage([FromForm] IFormFile photo)
    {
        try
        {
            if (photo == null || photo.Length == 0)
                return BadRequest(new { statusCode = 400, message = "Photo file is required" });

            using var ms = new MemoryStream();
            await photo.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());

            var dto = new GeminiDetectDto { Photo = base64 };
            var result = await _geminiService.DetectImageAsync(dto);

            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message, error = result.data });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, severity = data.severity, reason = data.reason });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in DetectImage form-data");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost("compareImage")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public async Task<IActionResult> CompareImage([FromForm] CompareImageDto dto)
    {
        try
        {
            if (dto.ProofImage == null || dto.ProofImage.Length == 0)
                return BadRequest(new { statusCode = 400, message = "img-1 (proof image) is required as file" });

            if (string.IsNullOrWhiteSpace(dto.ComplaintId))
                return BadRequest(new { statusCode = 400, message = "complaintId is required" });

            var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == dto.ComplaintId);
            if (complaint == null)
                return NotFound(new { statusCode = 404, message = "Original complaint not found (img-2 missing)" });

            var originalBase64 = complaint.PhotoBase64;

            using var ms = new MemoryStream();
            await dto.ProofImage.CopyToAsync(ms);
            var proofBase64 = Convert.ToBase64String(ms.ToArray());

            var result = await _geminiService.CompareImagesAsync(originalBase64, proofBase64);

            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message, data = result.data });

            var data = (dynamic)result.data!;
            return Ok(new
            {
                statusCode = 200,
                status = data.status,
                mssg = data.mssg,
                reason = data.reason,
                isFake = data.isFake,
                confidence = data.confidence,
                complaintId = dto.ComplaintId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in compareImage");
            return StatusCode(500, new { statusCode = 500, message = "Error comparing images", error = ex.Message });
        }
    }

    public class CompareImageDto
    {
        public string ComplaintId { get; set; } = "";
        public IFormFile ProofImage { get; set; } = null!;
    }

    [HttpPost("detectImageJson")]
    public async Task<IActionResult> DetectImageJson([FromBody] GeminiDetectDto dto)
    {
        try
        {
            var result = await _geminiService.DetectImageAsync(dto);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message, error = result.data });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, severity = data.severity, reason = data.reason });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in DetectImageJson");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }
}
