using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class UserUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
}
