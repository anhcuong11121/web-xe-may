using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class ProfileUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}
