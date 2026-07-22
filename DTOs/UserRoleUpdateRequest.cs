using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class UserRoleUpdateRequest
{
    [Required]
    public string Role { get; set; } = string.Empty;
}
