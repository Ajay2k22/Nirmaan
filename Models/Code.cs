using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Nirmaan.Api.Models;

public class Code
{
    [Key]
    public string? CodeId { get; set; }

    [Required, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, MaxLength(20)]
    public string UniqueCode { get; set; } = "";

    public DateTime Expiry { get; set; } = DateTime.UtcNow.AddMinutes(15);
    public bool IsUsed { get; set; } = false;
}