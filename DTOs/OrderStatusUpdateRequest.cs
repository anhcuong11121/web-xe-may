using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class OrderStatusUpdateRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;
}
