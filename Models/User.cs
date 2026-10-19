using System.ComponentModel.DataAnnotations;

namespace Nirmaan.Api.Models;

public class User
{
    [Key]
    [MaxLength(100)]
    public string UserId { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    public bool IsCitizen { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}