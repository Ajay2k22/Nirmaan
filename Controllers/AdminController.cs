using Microsoft.AspNetCore.Mvc;
using Nirmaan.Api.Interface;
using Microsoft.AspNetCore.Authorization;

namespace Nirmaan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] AdminRegDto dto)
    {
        try
        {
            var result = await _adminService.RegisterAsync(dto);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message, error = result.data });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, token = data.token, isCitizen = data.isCitizen });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in Admin Register");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AdminLoginDto dto)
    {
        try
        {
            var result = await _adminService.LoginAsync(dto);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, token = data.token, isCitizen = data.isCitizen, mnc_code = data.mnc_code, dept_code = data.dept_code });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in Admin Login");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("getAllComplaints")]
    [Authorize(Policy = "IsAdmin")]
    public async Task<IActionResult> GetAllComplaints([FromQuery] string mnc_code, [FromQuery] string dept_code)
    {
        try
        {

            var result = await _adminService.GetAllComplaintsAsync(mnc_code, dept_code);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, count = data.count, data = data.data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in GetAllComplaints");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("complaints")]
    [Authorize]
    public async Task<IActionResult> GetComplaintById([FromQuery] string mnc_code, [FromQuery] string dept_code, [FromQuery] string complaintId)
    {
        try
        {
            var result = await _adminService.GetComplaintByIdAsync(mnc_code, dept_code, complaintId);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

            return Ok(new { statusCode = result.statusCode, message = result.message, data = result.data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in GetComplaintById");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost("updateComplaintStatus")]
    [Authorize(Policy = "IsAdmin")]
    public async Task<IActionResult> UpdateComplaintStatus([FromBody] UpdateStatusDto dto)
    {
        try
        {
            var result = await _adminService.UpdateStatusAsync(dto);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, status = data.status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in UpdateComplaintStatus");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("getAllMasterData")]
    [Authorize]
    public async Task<IActionResult> GetAllMasterData()
    {
        try
        {
            var result = await _adminService.GetAllMasterDataAsync();
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

            var data = (dynamic)result.data!;
            return Ok(new { statusCode = result.statusCode, message = result.message, count = data.count, data = data.data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in GetAllMasterData");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }
    // [HttpPost("resolveAsync")]
    // [Authorize]
    // public async Task<IActionResult> Resolve([FromBody] ResolveComplaintDto dto)
    // {
    //     try
    //     {
    //         // Validation as per your API spec
    //         if (string.IsNullOrEmpty(dto.ComplaintId))
    //             return BadRequest(new { statusCode = 400, message = "complaintId required" });

    //         if (string.IsNullOrEmpty(dto.Proof_Img))
    //             return BadRequest(new { statusCode = 400, message = "proof_img required" });

    //         if (string.IsNullOrEmpty(dto.Summary_Of_Issue))
    //             return BadRequest(new { statusCode = 400, message = "summary_of_issue required" });

    //         var result = await _adminService.ResolveComplaintAsync(dto);

    //         if (!result.success)
    //             return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

    //         // Response :- { status } as per your diagram
    //         return Ok(new { status = dto.Status });
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error in resolve");
    //         return StatusCode(500, new { statusCode = 500, message = ex.Message });
    //     }
    // }

    // Extra: For Postman file upload testing (same API but with file)
    [HttpPost("resolve")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = "IsAdmin")]
    public async Task<IActionResult> ResolveForm([FromForm] ResolveComplaintFormDto formDto)
    {
        try
        {
            string base64 = "";
            if (formDto.Proof_Img != null)
            {
                using var ms = new MemoryStream();
                await formDto.Proof_Img.CopyToAsync(ms);
                base64 = Convert.ToBase64String(ms.ToArray());
            }

            var dto = new ResolveComplaintDto
            {
                ComplaintId = formDto.ComplaintId,
                Proof_Img = base64,
                Summary_Of_Issue = formDto.Summary_Of_Issue,
                Status = formDto.Status
            };

            var result = await _adminService.ResolveComplaintAsync(dto);
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message });

            return Ok(new { status = dto.Status });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { statusCode = 500, message = ex.Message });
        }
    }

    [HttpPost("rejectComplaint")]
    [Authorize(Policy = "IsAdmin")]
    public async Task<IActionResult> RejectComplaint([FromBody] RejectComplaintDto dto)
    {
        var result = await _adminService.RejectComplaintAsync(dto);
        return StatusCode(result.statusCode, new
        {
            statusCode = result.statusCode,
            message = result.message,
            data = result.data
        });
    }
}
