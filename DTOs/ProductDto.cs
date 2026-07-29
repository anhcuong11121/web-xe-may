namespace MotorBikeShop.API.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public int? VehicleTypeId { get; set; }
    public string? VehicleTypeName { get; set; }
}
