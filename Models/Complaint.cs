using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Nirmaan.Api.Enums;

namespace Nirmaan.Api.Models;

public class Complaint
{
    [Key]
    [MaxLength(100)]
    public string ComplaintId { get; set; } = Guid.NewGuid().ToString(); //system

    [MaxLength(200)]
    public string Title { get; set; } = ""; //user

    [MaxLength(200)]
    public string Description { get; set; } = ""; //user

    [Column(TypeName = "text")]
    public string PhotoBase64 { get; set; } = ""; //user

    [MaxLength(50)]
    public string PhotoMimeType { get; set; } = "image/jpeg"; //user

    public double Longitude { get; set; } //user

    public double Latitude { get; set; } //user

    [MaxLength(50)]
    public string Category { get; set; } = CivicCategory.OTHER.ToString(); // NEW

    [MaxLength(20)]
    public string Severity { get; set; } = "medium"; // from Gemini 

    [MaxLength(500)]
    public string Summary { get; set; } = ""; // Gemini

    [MaxLength(20)]
    public string Status { get; set; } = ComplaintStatus.PENDING.ToString(); // System

    [MaxLength(500)]
    public string Reason { get; set; } = ""; // from Admin

    [Required, MaxLength(100)]
    public string UserId { get; set; } = ""; //System
    [MaxLength(50)]
    public string MNC { get; set; } = ""; //user

    [MaxLength(50)]
    public string DEPT { get; set; } = ""; //user

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //System

    [ForeignKey("UserId")]
    public User? User { get; set; }
    [MaxLength(500)] // NEW
    public string Address { get; set; } = ""; // User
    [Column(TypeName = "text")]
    public string? ResolvedProofImg { get; set; }//Admin
    [MaxLength(1000)]
    public string? SummaryOfIssue { get; set; } //Admin
    public DateTime? ResolvedAt { get; set; } //Admin
    [MaxLength(100)]
    public string? ResolvedBy { get; set; } //Admin
    public bool IsResolved { get; set; } //Admin
    public string? GeoHash { get; set; } //By System
}