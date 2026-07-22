namespace MotorBikeShop.API.DTOs;

public class ProductQueryParameters
{
    public string? Keyword { get; set; }
    public int? BrandId { get; set; }
    public int? VehicleTypeId { get; set; }
    public string? Status { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
