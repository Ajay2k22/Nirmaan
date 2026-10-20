using Microsoft.AspNetCore.Mvc;
using Nirmaan.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Nirmaan.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
[ApiController]
[Route("api/fix")]
public class FixController : ControllerBase
{
    private readonly AppDbContext _db;
    public FixController(AppDbContext db) => _db = db;

    [HttpPost("updatePhoto/{complaintId}")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]

    public async Task<IActionResult> UpdatePhoto(string complaintId, [FromForm] IFormFile photo)
    {
        var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == complaintId);
        if (complaint == null) return NotFound();

        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms);
        complaint.PhotoBase64 = Convert.ToBase64String(ms.ToArray());
        await _db.SaveChangesAsync();

        return Ok(new { message = "Photo updated", complaintId, newLength = complaint.PhotoBase64.Length });
    }
}