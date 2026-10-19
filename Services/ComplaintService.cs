using Microsoft.EntityFrameworkCore;
using Nirmaan.Api.Data;
using Nirmaan.Api.Interface;
using Nirmaan.Api.Models;

namespace Nirmaan.Api.Services;

public class ComplaintService : IComplaintService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ComplaintService> _logger;

    public ComplaintService(AppDbContext db, ILogger<ComplaintService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(bool success, int statusCode, string message, object? data)> CreateAsync(CreateComplaintDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Photo))
                return (false, 400, "Photo is required", null);

            if (string.IsNullOrWhiteSpace(dto.MncCode) || string.IsNullOrWhiteSpace(dto.DeptCode))
                return (false, 400, "mnc_code and dept_code are required", null);

            if (string.IsNullOrWhiteSpace(dto.Summary))
                return (false, 400, "Summary is required", null);

            var complaint = new Complaint
            {
                ComplaintId = Guid.NewGuid().ToString(),
                PhotoBase64 = dto.Photo,
                MNC = dto.MncCode,
                DEPT = dto.DeptCode,
                Severity = dto.Severity ?? "medium",
                Longitude = dto.Longitude,
                Latitude = dto.Latitude,
                Address = dto.Address ?? "", // NEW
                Summary = dto.Summary,
                Reason = dto.Reason,
                Status = "pending",
                UserId = dto.UserId ?? "anonymous",
                CreatedAt = DateTime.UtcNow
            };

            _db.Complaints.Add(complaint);
            await _db.SaveChangesAsync();

            return (true, 200, "Complaint created successfully", new { successMssg = "Complaint created successfully", complaintId = complaint.ComplaintId });
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "DB error in Create Complaint UserId:{UserId}", dto.UserId);
            return (false, 500, "Database error while creating complaint", new { error = dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Create Complaint");
            return (false, 500, "Internal server error while creating complaint", new { error = ex.Message });
        }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> GetByUserIdAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                return (false, 400, "userId query param is required", null);

            var list = await _db.Complaints
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new { c.ComplaintId, photo = c.PhotoBase64, c.Status })
                .ToListAsync();

            return (true, 200, "Complaints fetched", new { count = list.Count, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByUserId UserId:{UserId}", userId);
            return (false, 500, "Internal server error while fetching complaints", new { error = ex.Message });
        }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> GetByUserAndComplaintIdAsync(string userId, string complaintId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(complaintId))
                return (false, 400, "userId and complaintId query params are required", null);

            var complaint = await _db.Complaints
                .Where(c => c.UserId == userId && c.ComplaintId == complaintId)
                .Select(c => new { c.ComplaintId, photo = c.PhotoBase64, c.Status, c.Address, c.Severity, c.Summary, c.Longitude, c.Latitude, c.MNC, c.DEPT, c.Reason })
                .FirstOrDefaultAsync();

            if (complaint == null)
                return (false, 404, "Complaint not found", null);

            return (true, 200, "Complaint fetched", complaint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByUserAndComplaintId User:{UserId} Complaint:{ComplaintId}", userId, complaintId);
            return (false, 500, "Internal server error", new { error = ex.Message });
        }
    }

}
