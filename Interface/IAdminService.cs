namespace Nirmaan.Api.Interface;

public record AdminRegDto(string MncCode, string DeptCode, string Pwd);
public record AdminLoginDto(string MncCode, string DeptCode, string Pwd);
public record UpdateStatusDto(string Status, string ComplaintId);

public class ResolveComplaintDto
{
    public string ComplaintId { get; set; } = "";
    public string Proof_Img { get; set; } = "";
    public string Summary_Of_Issue { get; set; } = "";
    public string Status { get; set; } = "resolved";
}

public class ResolveComplaintFormDto
{
    public string ComplaintId { get; set; } = "";
    public IFormFile? Proof_Img { get; set; }
    public string Summary_Of_Issue { get; set; } = "";
    public string Status { get; set; } = "resolved";
}

public class RejectComplaintDto
{
    public string ComplaintId { get; set; } = "";
    public string? Reason { get; set; }
}

public interface IAdminService
{
    // CHANGED: Now accepts List for bulk insert
    Task<(bool success, int statusCode, string message, object? data)> RegisterAsync(List<AdminRegDto> dtos);

    Task<(bool success, int statusCode, string message, object? data)> LoginAsync(AdminLoginDto dto);
    Task<(bool success, int statusCode, string message, object? data)> GetAllComplaintsAsync(string mncCode, string deptCode);
    Task<(bool success, int statusCode, string message, object? data)> GetComplaintByIdAsync(string mncCode, string deptCode, string complaintId);
    Task<(bool success, int statusCode, string message, object? data)> UpdateStatusAsync(UpdateStatusDto dto);
    Task<(bool success, int statusCode, string message, object? data)> GetAllMasterDataAsync();
    Task<(bool success, int statusCode, string message, object? data)> ResolveComplaintAsync(ResolveComplaintDto dto);
    Task<(bool success, int statusCode, string message, object? data)> RejectComplaintAsync(RejectComplaintDto dto);
}
