using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class BrandUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [Url]
    [MaxLength(500)]
    public string? LogoUrl { get; set; }
}
