namespace MotorBikeShop.API.DTOs;

public class SpecificationDto
{
    public string EngineType { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public int EngineCapacityCc { get; set; }
    public int HorsePower { get; set; }
    public decimal? CurbWeightKg { get; set; }
    public string? Dimensions { get; set; }
    public decimal? FuelTankCapacityLiters { get; set; }
    public string? MaxPower { get; set; }
    public decimal? FuelConsumptionLitersPer100Km { get; set; }
    public string? OtherDetails { get; set; }
}
