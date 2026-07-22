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

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Required]
    [MaxLength(100)]
    public string Color { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Available";

    [Required]
    public int BrandId { get; set; }

    public int? VehicleTypeId { get; set; }

    [Required]
    public SpecificationCreateRequest Specification { get; set; } = new();
}
