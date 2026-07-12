using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class SupportRequest
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Open";

    public virtual AppUser User { get; set; } = null!;
}
