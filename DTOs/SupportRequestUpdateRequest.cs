using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class SupportRequestUpdateRequest
{
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Response { get; set; }
}
