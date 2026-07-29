using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class ProductCreateRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Available";

    [Required]
    public int BrandId { get; set; }

    public int? VehicleTypeId { get; set; }
}
