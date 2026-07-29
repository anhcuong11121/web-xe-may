using MotorBikeShop.API.DTOs;
using MotorBikeShop.API.Models;

namespace MotorBikeShop.API.Services;

internal static class ProductCatalogMapper
{
    public static ProductVariantDto MapVariant(ProductVariant variant, bool includeInactiveSkus)
    {
        return new ProductVariantDto
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Name = variant.Name,
            VersionCode = variant.VersionCode,
            Status = variant.Status,
            Specification = variant.Specification == null
                ? null
                : new VariantSpecificationDto
                {
                    EngineType = variant.Specification.EngineType,
                    FuelType = variant.Specification.FuelType,
                    EngineCapacityCc = variant.Specification.EngineCapacityCc,
                    HorsePower = variant.Specification.HorsePower,
                    CurbWeightKg = variant.Specification.CurbWeightKg,
                    Dimensions = variant.Specification.Dimensions,
                    FuelTankCapacityLiters = variant.Specification.FuelTankCapacityLiters,
                    MaxPower = variant.Specification.MaxPower,
                    FuelConsumptionLitersPer100Km =
                        variant.Specification.FuelConsumptionLitersPer100Km,
                    OtherDetails = variant.Specification.OtherDetails
                },
            Skus = variant.Skus
                .Where(sku => includeInactiveSkus || sku.Status == CatalogStatuses.Active)
                .OrderBy(sku => sku.Id)
                .Select(MapSku)
                .ToList()
        };
    }

    public static ProductSkuDto MapSku(ProductSku sku)
    {
        return new ProductSkuDto
        {
            Id = sku.Id,
            ProductVariantId = sku.ProductVariantId,
            SkuCode = sku.SkuCode,
            ColorName = sku.ColorName,
            ColorHexCode = sku.ColorHexCode,
            Price = sku.Price,
            StockQuantity = sku.StockQuantity,
            Status = sku.Status,
            RowVersion = Convert.ToBase64String(sku.RowVersion),
            Images = sku.Images
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(MapImage)
                .ToList()
        };
    }

    public static ProductImageDto MapImage(ProductImage image)
    {
        return new ProductImageDto
        {
            Id = image.Id,
            ProductSkuId = image.ProductSkuId,
            Url = image.Url,
            AltText = image.AltText,
            DisplayOrder = image.DisplayOrder,
            IsPrimary = image.IsPrimary
        };
    }
}
