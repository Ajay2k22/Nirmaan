using Microsoft.EntityFrameworkCore;
using Nirmaan.Api.Data;
using Nirmaan.Api.Interface;
using Nirmaan.Api.Models;

namespace Nirmaan.Api.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwtService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(AppDbContext db, IJwtTokenService jwtService, ILogger<AdminService> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<(bool success, int statusCode, string message, object? data)> RegisterAsync(AdminRegDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.MncCode) || string.IsNullOrWhiteSpace(dto.DeptCode) || string.IsNullOrWhiteSpace(dto.Pwd))
                return (false, 400, "mnc_code, dept_code and pwd are required", null);

            if (await _db.MasterDatas.AnyAsync(m => m.MNC == dto.MncCode && m.DEPT == dto.DeptCode))
                return (false, 409, "MNC+DEPT already exists", null);

            var md = new MasterData
            {
                MasterDataId = Guid.NewGuid().ToString(),
                MNC = dto.MncCode,
                DEPT = dto.DeptCode,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Pwd),
                IsCitizen = false
            };

            _db.MasterDatas.Add(md);
            await _db.SaveChangesAsync();

            var token = _jwtService.GenerateAdminToken(md.MasterDataId, md.MNC, md.DEPT);
            return (true, 200, "Admin registered successfully", new { token, isCitizen = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Admin Register");
            return (false, 500, "Internal server error", new { error = ex.Message });
        }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> LoginAsync(AdminLoginDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.MncCode) || string.IsNullOrWhiteSpace(dto.DeptCode) || string.IsNullOrWhiteSpace(dto.Pwd))
                return (false, 400, "mnc_code, dept_code and pwd are required", null);

            var md = await _db.MasterDatas.FirstOrDefaultAsync(m => m.MNC == dto.MncCode && m.DEPT == dto.DeptCode);
            if (md == null)
                return (false, 404, "Admin not found for given MNC and DEPT", null);

            if (!BCrypt.Net.BCrypt.Verify(dto.Pwd, md.Password))
                return (false, 401, "Invalid admin credentials", null);

            var token = _jwtService.GenerateAdminToken(md.MasterDataId, md.MNC, md.DEPT);
            return (true, 200, "Admin login successful", new { token, isCitizen = false, mnc_code = md.MNC, dept_code = md.DEPT });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Admin Login");
            return (false, 500, "Internal server error", new { error = ex.Message });
        }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> GetAllComplaintsAsync(string mncCode, string deptCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(mncCode) || string.IsNullOrWhiteSpace(deptCode))
                return (false, 400, "mnc_code and dept_code required", null);

            var list = await _db.Complaints
                .Where(c => c.MNC == mncCode && c.DEPT == deptCode)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new { c.UserId, photo = c.PhotoBase64, c.Status, c.Severity, c.Longitude, c.Latitude, c.Reason, c.ComplaintId, c.Summary, c.CreatedAt })
                .ToListAsync();

            return (true, 200, "Complaints fetched", new { count = list.Count, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllComplaints");
            return (false, 500, "Internal server error", new { error = ex.Message });
        }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> GetComplaintByIdAsync(string mncCode, string deptCode, string complaintId)
    {
        try
        {
            var c = await _db.Complaints
                .Where(x => x.MNC == mncCode && x.DEPT == deptCode && x.ComplaintId == complaintId)
                .Select(x => new { x.UserId, x.Longitude, x.Latitude, photo = x.PhotoBase64, x.Severity, x.Summary, x.Status, x.Reason, x.ComplaintId, x.MNC, x.DEPT })
                .FirstOrDefaultAsync();

            if (c == null)
                return (false, 404, "Complaint not found", null);

            return (true, 200, "Complaint fetched", c);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetComplaintById");
            return (false, 500, "Internal server error", new { error = ex.Message });
        }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> UpdateStatusAsync(UpdateStatusDto dto)
    {
        try
        {
            var complaint = await _db.Complaints.FirstOrDefaultAsync(x => x.ComplaintId == dto.ComplaintId);
            if (complaint == null)
                return (false, 404, "Complaint not found", null);

            complaint.Status = dto.Status;
            await _db.SaveChangesAsync();

            return (true, 200, "Complaint status updated", new { status = complaint.Status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateStatus");
            return (false, 500, "Internal server error", new { error = ex.Message });
        }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> GetAllMasterDataAsync()
    {
        try
        {
            var list = await _db.MasterDatas
                .Select(m => new { mnc_code = m.MNC, dept_code = m.DEPT, m.MasterDataId })
                .ToListAsync();

            return (true, 200, "Master data fetched", new { count = list.Count, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllMasterData");
            return (false, 500, "Internal server error", new { error = ex.Message });
        }
    }

    // --- NEW RESOLVE METHOD - FIXED ---
    public async Task<(bool success, int statusCode, string message, object? data)> ResolveComplaintAsync(ResolveComplaintDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.ComplaintId))
                return (false, 400, "complaintId is required", null);

            // FIX: Use _db.Complaints NOT _complaintService.Complaints
            var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == dto.ComplaintId);
            
            if (complaint == null)
                return (false, 404, "Complaint not found", null);

            if (complaint.Status == "resolved")
                return (false, 400, "Complaint already resolved", new { status = complaint.Status });

            // Update as per your API: { proof_img, summary_of_issue, status }
            complaint.Status = string.IsNullOrWhiteSpace(dto.Status) ? "resolved" : dto.Status.ToLower();
            complaint.ResolvedProofImg = dto.Proof_Img;
            complaint.SummaryOfIssue = dto.Summary_Of_Issue;
            complaint.ResolvedAt = DateTime.UtcNow;
            complaint.Reason = dto.Summary_Of_Issue; // also update reason for display

            await _db.SaveChangesAsync();

            _logger.LogInformation("Complaint {Id} resolved", dto.ComplaintId);

            // Response :- { status } as per diagram
            return (true, 200, "Resolved", new { status = complaint.Status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResolveComplaint {Id}", dto.ComplaintId);
            return (false, 500, "Failed to resolve complaint", new { error = ex.Message });
        }
    }
}
