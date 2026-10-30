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

    // ========= BULK REGISTER - NOW ACCEPTS LIST =========
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] List<AdminRegDto> dtos)
    {
        try
        {
            if (dtos == null || dtos.Count == 0)
                return BadRequest(new { statusCode = 400, message = "Payload list is empty" });

            var result = await _adminService.RegisterAsync(dtos);

            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message, error = result.data });

            // result.data contains insertedCount, skippedCount, inserted, skipped
            return Ok(new
            {
                statusCode = result.statusCode,
                message = result.message,
                data = result.data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in Bulk Admin Register");
            return StatusCode(500, new { statusCode = 500, message = "Internal server error", error = ex.Message });
        }
    }

    // Optional: Single register also works via list (if you send single object, wrap it in list)
    // If you want to support BOTH single object and list, use this extra endpoint
    [HttpPost("register-single")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterSingle([FromBody] AdminRegDto dto)
    {
        try
        {
            var result = await _adminService.RegisterAsync(new List<AdminRegDto> { dto });
            if (!result.success)
                return StatusCode(result.statusCode, new { statusCode = result.statusCode, message = result.message, error = result.data });

            return Ok(new { statusCode = result.statusCode, message = result.message, data = result.data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in Single Admin Register");
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


    [HttpPost("resolve")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = "IsAdmin")]
    public async Task<IActionResult> ResolveForm([FromForm] ResolveComplaintFormDto formDto)
    {
        try
        {
            if (formDto.Proof_Img == null)
                return BadRequest(new { statusCode = 400, message = "Proof image required", status = "REJECTED", mssg = "Proof missing", reason = "Proof missing", isFake = false, confidence = "low", complaintId = formDto.ComplaintId });

            string base64 = "";
            using (var ms = new MemoryStream())
            {
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

            // ✅ FIX: Handle null data safely
            if (result.data == null)
            {
                return StatusCode(result.statusCode, new
                {
                    statusCode = result.statusCode,
                    message = result.message,
                    status = "REJECTED",
                    mssg = result.message,
                    reason = result.message,
                    isFake = false,
                    confidence = "low",
                    complaintId = formDto.ComplaintId
                });
            }

            var json = System.Text.Json.JsonSerializer.Serialize(result.data);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            // If ai.data is already flat {status,mssg,...} not wrapped
            string status = "REJECTED", mssg = result.message, reason = result.message, confidence = "low";
            bool isFake = false;

            if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (root.TryGetProperty("verification", out var ver) && ver.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    status = ver.TryGetProperty("status", out var s) ? s.GetString() ?? status : status;
                    mssg = ver.TryGetProperty("mssg", out var mm) ? mm.GetString() ?? mssg : mssg;
                    reason = ver.TryGetProperty("reason", out var rr) ? rr.GetString() ?? reason : reason;
                    confidence = ver.TryGetProperty("confidence", out var cc) ? cc.GetString() ?? confidence : confidence;
                    isFake = ver.TryGetProperty("isFake", out var ff) && ff.ValueKind == System.Text.Json.JsonValueKind.True;
                }
                else
                {
                    // Direct object {status,mssg,reason,isFake,confidence}
                    status = root.TryGetProperty("status", out var s2) ? s2.GetString() ?? status : status;
                    mssg = root.TryGetProperty("mssg", out var m2) ? m2.GetString() ?? mssg : mssg;
                    reason = root.TryGetProperty("reason", out var r2) ? r2.GetString() ?? reason : reason;
                    confidence = root.TryGetProperty("confidence", out var c2) ? c2.GetString() ?? confidence : confidence;
                    isFake = root.TryGetProperty("isFake", out var f2) && f2.ValueKind == System.Text.Json.JsonValueKind.True;
                }
            }

            return StatusCode(result.statusCode, new
            {
                statusCode = result.statusCode,
                message = result.message,
                status,
                mssg,
                reason,
                isFake,
                confidence,
                complaintId = formDto.ComplaintId
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { statusCode = 500, message = ex.Message, status = "REJECTED", mssg = ex.Message, reason = ex.Message, isFake = false, confidence = "low", complaintId = formDto.ComplaintId });
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
