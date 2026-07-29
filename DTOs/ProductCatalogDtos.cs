namespace MotorBikeShop.API.DTOs;

public class ProductCatalogSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public int? VehicleTypeId { get; set; }
    public string? VehicleTypeName { get; set; }
    public decimal? MinimumPrice { get; set; }
    public decimal? MaximumPrice { get; set; }
    public int? MinimumEngineCapacityCc { get; set; }
    public int? MaximumEngineCapacityCc { get; set; }
    public long TotalStock { get; set; }
    public int AvailableSkuCount { get; set; }
    public string? PrimaryImageUrl { get; set; }
}

public class ProductCatalogDetailDto : ProductCatalogSummaryDto
{
    public List<ProductVariantDto> Variants { get; set; } = new();
}

public class ProductVariantDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VersionCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public VariantSpecificationDto? Specification { get; set; }
    public List<ProductSkuDto> Skus { get; set; } = new();
}

public class VariantSpecificationDto
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

public class ProductSkuDto
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string? ColorHexCode { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
    public List<ProductImageDto> Images { get; set; } = new();
}

public class ProductImageDto
{
    public int Id { get; set; }
    public int ProductSkuId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}
