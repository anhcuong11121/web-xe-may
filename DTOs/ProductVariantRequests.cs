using System.ComponentModel.DataAnnotations;

namespace MotorBikeShop.API.DTOs;

public class ProductVariantCreateRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string VersionCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = "Active";

    [Required]
    public VariantSpecificationRequest Specification { get; set; } = new();
}

public class ProductVariantUpdateRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = "Active";

    [Required]
    public VariantSpecificationRequest Specification { get; set; } = new();
}

public class VariantSpecificationRequest
{
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
}

public class ProductVariantDeleteDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Status { get; set; }
}
