using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nirmaan.Api.Models;

public class Complaint
{
    [Key]
    [MaxLength(100)]
    public string ComplaintId { get; set; } = Guid.NewGuid().ToString();

    // COLLEGE VERSION: No Cloudflare - store compressed base64 directly
    // Angular will compress to 800px, 0.6 quality = ~150KB
    [Column(TypeName = "text")]
    public string PhotoBase64 { get; set; } = "";

    [MaxLength(50)]
    public string PhotoMimeType { get; set; } = "image/jpeg";

    public double Longitude { get; set; }
    public double Latitude { get; set; }

    [MaxLength(20)]
    public string Severity { get; set; } = "medium"; // from Gemini

    [MaxLength(500)]
    public string Summary { get; set; } = "";

    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, in_progress, resolved

    [MaxLength(500)]
    public string Reason { get; set; } = ""; // from Gemini

    [Required, MaxLength(100)]
    public string UserId { get; set; } = "";

    [MaxLength(50)]
    public string MNC { get; set; } = "";

    [MaxLength(50)]
    public string DEPT { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public User? User { get; set; }
    [MaxLength(500)] // NEW
    public string Address { get; set; } = ""; // Full address from reverse geocoding
    [Column(TypeName = "text")]
    public string? ResolvedProofImg { get; set; } // proof_img
    [MaxLength(1000)]
    public string? SummaryOfIssue { get; set; } // summary_of_issue - how fixed
    public DateTime? ResolvedAt { get; set; }
    [MaxLength(100)]
    public string? ResolvedBy { get; set; }
    public bool isResolved { get; set; }
    public string? GeoHash { get; set; }
}