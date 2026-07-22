using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
