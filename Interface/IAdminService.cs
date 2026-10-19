

namespace Nirmaan.Api.Interface;

public record AdminRegDto(string MncCode, string DeptCode, string Pwd);
public record AdminLoginDto(string MncCode, string DeptCode, string Pwd);
public record UpdateStatusDto(string Status, string ComplaintId);
public class ResolveComplaintDto
{
    public string ComplaintId { get; set; } = ""; // which complaint
    public string Proof_Img { get; set; } = ""; // base64 string for JSON
    public string Summary_Of_Issue { get; set; } = ""; // resolution description
    public string Status { get; set; } = "resolved"; // should be resolved
}

// For form-data file upload (Postman testing)
public class ResolveComplaintFormDto
{
    public string ComplaintId { get; set; } = "";
    public IFormFile? Proof_Img { get; set; } // file upload
    public string Summary_Of_Issue { get; set; } = "";
    public string Status { get; set; } = "resolved";
}

public interface IAdminService
{
    Task<(bool success, int statusCode, string message, object? data)> RegisterAsync(AdminRegDto dto);
    Task<(bool success, int statusCode, string message, object? data)> LoginAsync(AdminLoginDto dto);
    Task<(bool success, int statusCode, string message, object? data)> GetAllComplaintsAsync(string mncCode, string deptCode);
    Task<(bool success, int statusCode, string message, object? data)> GetComplaintByIdAsync(string mncCode, string deptCode, string complaintId);
    Task<(bool success, int statusCode, string message, object? data)> UpdateStatusAsync(UpdateStatusDto dto);
    Task<(bool success, int statusCode, string message, object? data)> GetAllMasterDataAsync();
    Task<(bool success, int statusCode, string message, object? data)> ResolveComplaintAsync(ResolveComplaintDto dto);
}
