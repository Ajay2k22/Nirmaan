
namespace Nirmaan.Api.Interface;

public class CreateComplaintDto
{
    public string Photo { get; set; } = "";
    public string MncCode { get; set; } = "";
    public string DeptCode { get; set; } = "";
    public string? Severity { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public string Summary { get; set; } = "";
    public string? UserId { get; set; }
    public string Address { get; set; } = ""; // NEW
    public string Reason { get; set; } = ""; // NEW
}

public interface IComplaintService
{
    Task<(bool success, int statusCode, string message, object? data)> CreateAsync(CreateComplaintDto dto);
    Task<(bool success, int statusCode, string message, object? data)> GetByUserIdAsync(string userId);
    Task<(bool success, int statusCode, string message, object? data)> GetByUserAndComplaintIdAsync(string userId, string complaintId);
    
}
