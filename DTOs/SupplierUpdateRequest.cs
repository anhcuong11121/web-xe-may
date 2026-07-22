using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class SupplierUpdateRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ContactPerson { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";
}
