using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class SupportRequestCreateRequest
{
    [Required]
    [MaxLength(50)]
    public string SupportType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;
}
