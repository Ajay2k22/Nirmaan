using Microsoft.AspNetCore.Mvc;
using Nirmaan.Api.Interface;
using Microsoft.AspNetCore.Authorization;

namespace Nirmaan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComplaintController : ControllerBase
{
    private readonly IComplaintService _complaintService;
    private readonly ILogger<ComplaintController> _logger;

    public ComplaintController(IComplaintService complaintService, ILogger<ComplaintController> logger)
    {
        _complaintService = complaintService;
        _logger = logger;
    }

    [HttpPost("create")]
    [Authorize(Policy = "IsCitizen")]
    public async Task<IActionResult> Create([FromBody] CreateComplaintDto dto)
    {
        try
        {
            var result = await _complaintService.CreateAsync(dto);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message, error = result.data });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, successMssg = data.successMssg, complaintId = data.complaintId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in Create Complaint");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error while creating complaint", error = ex.Message });
        }
    }

    [HttpGet("getComplaintByUserId")]
    [Authorize(Policy = "IsCitizen")]
    public async Task<IActionResult> GetComplaintByUserId([FromQuery] string userId)
    {
        try
        {
            var result = await _complaintService.GetByUserIdAsync(userId);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, count = data.count, data = data.data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in GetComplaintByUserId");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("getComplaintByUserAndComplaintId")]
    [Authorize(Policy = "IsCitizen")]
    public async Task<IActionResult> GetComplaintByUserAndComplaintId([FromQuery] string userId, [FromQuery] string complaintId)
    {
        try
        {
            var result = await _complaintService.GetByUserAndComplaintIdAsync(userId, complaintId);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

            return Ok(new { statusCode = result.statusCode, message = result.message, data = result.data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in GetComplaintByUserAndComplaintId");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }
}
