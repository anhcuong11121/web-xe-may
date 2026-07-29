using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.Models;

public class VariantSpecification
{
    [Key]
    public int ProductVariantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string EngineType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FuelType { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int EngineCapacityCc { get; set; }

    [Range(0, int.MaxValue)]
    public int HorsePower { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CurbWeightKg { get; set; }

    [MaxLength(100)]
    public string? Dimensions { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? FuelTankCapacityLiters { get; set; }

    [MaxLength(100)]
    public string? MaxPower { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? FuelConsumptionLitersPer100Km { get; set; }

    [MaxLength(2000)]
    public string? OtherDetails { get; set; }

    public virtual ProductVariant ProductVariant { get; set; } = null!;
}
