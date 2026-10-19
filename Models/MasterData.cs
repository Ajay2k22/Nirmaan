using System.ComponentModel.DataAnnotations;

namespace Nirmaan.Api.Models;

public class MasterData
{
    [Key]
    [MaxLength(100)]
    public string MasterDataId { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(50)]
    public string MNC { get; set; } = "";

    [Required, MaxLength(50)]
    public string DEPT { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    public bool IsCitizen { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
