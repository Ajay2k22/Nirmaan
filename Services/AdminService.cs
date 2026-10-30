using Microsoft.EntityFrameworkCore;
using Nirmaan.Api.Data;
using Nirmaan.Api.Interface;
using Nirmaan.Api.Models;
using Nirmaan.Api.Enums;

namespace Nirmaan.Api.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwtService;
    private readonly ILogger<AdminService> _logger;
    private readonly IGeminiService _geminiService;

    public AdminService(AppDbContext db, IJwtTokenService jwtService, ILogger<AdminService> logger, IGeminiService geminiService)
    {
        _db = db;
        _jwtService = jwtService;
        _logger = logger;
        _geminiService = geminiService;
    }

    // ============= BULK REGISTER - NEW =============
    public async Task<(bool success, int statusCode, string message, object? data)> RegisterAsync(List<AdminRegDto> dtos)
    {
        try
        {
            if (dtos == null || dtos.Count == 0)
                return (false, 400, "Payload list is empty", null);

            var inserted = new List<object>();
            var skipped = new List<object>();
            var toInsert = new List<MasterData>();

            foreach (var dto in dtos)
            {
                if (string.IsNullOrWhiteSpace(dto.MncCode) || string.IsNullOrWhiteSpace(dto.DeptCode) || string.IsNullOrWhiteSpace(dto.Pwd))
                {
                    skipped.Add(new { dto.MncCode, dto.DeptCode, reason = "Missing fields" });
                    continue;
                }

                // Check duplicate
                bool exists = await _db.MasterDatas.AnyAsync(m => m.MNC == dto.MncCode && m.DEPT == dto.DeptCode);
                if (exists)
                {
                    skipped.Add(new { dto.MncCode, dto.DeptCode, reason = "Already exists" });
                    continue;
                }

                var md = new MasterData
                {
                    MasterDataId = Guid.NewGuid().ToString(),
                    MNC = dto.MncCode.Trim(),
                    DEPT = dto.DeptCode.Trim(),
                    Password = BCrypt.Net.BCrypt.HashPassword(dto.Pwd),
                    IsCitizen = false,
                    CreatedAt = DateTime.UtcNow
                };

                toInsert.Add(md);
                inserted.Add(new { md.MNC, md.DEPT, md.MasterDataId });
            }

            if (toInsert.Count > 0)
            {
                _db.MasterDatas.AddRange(toInsert);
                await _db.SaveChangesAsync();
            }

            _logger.LogInformation("Bulk Register: Inserted {Inserted}, Skipped {Skipped}", inserted.Count, skipped.Count);

            return (true, 200, $"Bulk register completed. Inserted: {inserted.Count}, Skipped: {skipped.Count}",
                new
                {
                    insertedCount = inserted.Count,
                    skippedCount = skipped.Count,
                    inserted,
                    skipped
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Bulk Admin Register");
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
                .Select(c => new { c.UserId, photo = c.PhotoBase64, c.Status, c.Severity, c.Longitude, c.Latitude, c.Reason, c.ComplaintId, c.Summary, c.CreatedAt, c.Title, c.Description, c.Category })
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
                .Select(x => new { x.UserId, x.Longitude, x.Latitude, photo = x.PhotoBase64, x.Severity, x.Summary, x.Status, x.Reason, x.ComplaintId, x.MNC, x.DEPT, x.Title, x.Description, x.Category, x.CreatedAt, x.ResolvedProofImg, x.IsResolved, x.ResolvedAt })
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

            if (Enum.TryParse<ComplaintStatus>(dto.Status, true, out var parsedStatus))
                complaint.Status = parsedStatus.ToString();
            else
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


    public async Task<(bool success, int statusCode, string message, object? data)> ResolveComplaintAsync(ResolveComplaintDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.ComplaintId))
                return (false, 400, "complaintId required",
                    new { status = "REJECTED", mssg = "ID missing", reason = "ID missing", isFake = false, confidence = "low" });

            var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == dto.ComplaintId);
            if (complaint == null)
                return (false, 404, "Complaint not found",
                    new { status = "REJECTED", mssg = "Not found", reason = "Not found", isFake = false, confidence = "low" });

            var incomingStatus = dto.Status?.ToUpper() ?? "RESOLVED";

            // ✅ NEW: If user passes REJECTED, skip AI verification, directly reject
            if (incomingStatus == "REJECTED")
            {
                complaint.Status = ComplaintStatus.REJECTED.ToString();
                complaint.Reason = dto.Summary_Of_Issue;
                complaint.ResolvedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return (true, 200, "Complaint rejected",
                    new { status = "REJECTED", mssg = "Complaint rejected by officer", reason = dto.Summary_Of_Issue ?? "Rejected", isFake = false, confidence = "high" });
            }

            // ✅ Only for RESOLVED, call AI
            if (string.IsNullOrWhiteSpace(dto.Proof_Img))
                return (false, 400, "Proof image required for resolve",
                    new { status = "REJECTED", mssg = "Proof missing", reason = "Proof missing", isFake = false, confidence = "low" });

            var ai = await _geminiService.CompareImagesAsync(complaint.PhotoBase64, dto.Proof_Img);
            if (!ai.success)
                return (false, 400, ai.message, ai.data);

            var el = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(ai.data)).RootElement;
            var aiStatus = el.TryGetProperty("status", out var s) ? s.GetString() : "REJECTED";

            if (aiStatus?.ToUpper() != "RESOLVED")
                return (false, 400, el.TryGetProperty("reason", out var r) ? r.GetString() : "Not resolved", ai.data);

            complaint.Status = ComplaintStatus.RESOLVED.ToString();
            complaint.ResolvedProofImg = "data:image/jpeg;base64," + dto.Proof_Img;
            complaint.SummaryOfIssue = dto.Summary_Of_Issue;
            complaint.ResolvedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return (true, 200, "Resolved - AI verified", ai.data);
        }
        catch (Exception ex)
        {
            return (false, 500, ex.Message,
                new { status = "REJECTED", mssg = ex.Message, reason = ex.Message, isFake = false, confidence = "low" });
        }
    }

    public async Task<(bool success, int statusCode, string message, object? data)> RejectComplaintAsync(RejectComplaintDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.ComplaintId))
                return (false, 400, "complaintId is required", null);
            var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == dto.ComplaintId);
            if (complaint == null)
                return (false, 404, "Complaint not found", null);
            if (complaint.Status == ComplaintStatus.RESOLVED.ToString())
                return (false, 400, "Complaint already resolved, cannot reject", new { status = complaint.Status });
            if (complaint.Status == ComplaintStatus.REJECTED.ToString())
                return (false, 400, "Complaint already rejected", new { status = complaint.Status });
            complaint.Status = ComplaintStatus.REJECTED.ToString();
            if (!string.IsNullOrWhiteSpace(dto.Reason))
                complaint.Reason = dto.Reason;
            await _db.SaveChangesAsync();
            return (true, 200, "Complaint rejected", new { status = complaint.Status, reason = complaint.Reason });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RejectComplaint {Id}", dto.ComplaintId);
            return (false, 500, "Failed to reject complaint", new { error = ex.Message });
        }
    }
    // public async Task<(bool success, int statusCode, string message, object? data)> RejectComplaintAsync(RejectComplaintDto dto)
    // {
    //     try
    //     {
    //         if (string.IsNullOrWhiteSpace(dto.ComplaintId))
    //             return (false, 400, "complaintId is required", null);

    //         var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == dto.ComplaintId);
    //         if (complaint == null)
    //             return (false, 404, "Complaint not found", null);

    //         if (complaint.Status == ComplaintStatus.RESOLVED.ToString())
    //             return (false, 400, "Complaint already resolved, cannot reject", new { status = complaint.Status });

    //         if (complaint.Status == ComplaintStatus.REJECTED.ToString())
    //             return (false, 400, "Complaint already rejected", new { status = complaint.Status });

    //         complaint.Status = ComplaintStatus.REJECTED.ToString();
    //         if (!string.IsNullOrWhiteSpace(dto.Reason))
    //             complaint.Reason = dto.Reason;

    //         await _db.SaveChangesAsync();
    //         return (true, 200, "Complaint rejected", new { status = complaint.Status, reason = complaint.Reason });
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error in RejectComplaint {Id}", dto.ComplaintId);
    //         return (false, 500, "Failed to reject complaint", new { error = ex.Message });
    //     }
    // }
}
