using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class BackfillLegacyProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET NOCOUNT ON;

                IF EXISTS
                (
                    SELECT 1
                    FROM Products AS product
                    INNER JOIN ProductSkus AS sku
                        ON sku.SkuCode = CONCAT(
                            'LEGACY-P',
                            RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
                            '-S01')
                    INNER JOIN ProductVariants AS variant
                        ON variant.Id = sku.ProductVariantId
                    WHERE variant.ProductId <> product.Id
                       OR variant.VersionCode <> CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
                )
                BEGIN
                    THROW 51000, 'Legacy SKU code collision detected. Backfill was cancelled.', 1;
                END;

                INSERT INTO ProductVariants (ProductId, Name, VersionCode, Status)
                SELECT
                    product.Id,
                    N'Phiên bản hiện tại',
                    CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id)),
                    N'Active'
                FROM Products AS product
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM ProductVariants AS variant
                    WHERE variant.ProductId = product.Id
                      AND variant.VersionCode = CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
                );

                INSERT INTO VariantSpecifications
                (
                    ProductVariantId,
                    EngineType,
                    FuelType,
                    EngineCapacityCc,
                    HorsePower,
                    CurbWeightKg,
                    Dimensions,
                    FuelTankCapacityLiters,
                    MaxPower,
                    FuelConsumptionLitersPer100Km,
                    OtherDetails
                )
                SELECT
                    variant.Id,
                    specification.EngineType,
                    specification.FuelType,
                    specification.EngineCapacityCc,
                    specification.HorsePower,
                    specification.CurbWeightKg,
                    specification.Dimensions,
                    specification.FuelTankCapacityLiters,
                    specification.MaxPower,
                    specification.FuelConsumptionLitersPer100Km,
                    specification.OtherDetails
                FROM Products AS product
                INNER JOIN ProductVariants AS variant
                    ON variant.ProductId = product.Id
                   AND variant.VersionCode = CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
                INNER JOIN Specifications AS specification
                    ON specification.ProductId = product.Id
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM VariantSpecifications AS existing
                    WHERE existing.ProductVariantId = variant.Id
                );

                INSERT INTO ProductSkus
                (
                    ProductVariantId,
                    SkuCode,
                    ColorName,
                    ColorHexCode,
                    Price,
                    StockQuantity,
                    Status
                )
                SELECT
                    variant.Id,
                    CONCAT(
                        'LEGACY-P',
                        RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
                        '-S01'),
                    COALESCE(NULLIF(LTRIM(RTRIM(product.Color)), N''), N'Chưa xác định'),
                    NULL,
                    product.Price,
                    product.StockQuantity,
                    N'Active'
                FROM Products AS product
                INNER JOIN ProductVariants AS variant
                    ON variant.ProductId = product.Id
                   AND variant.VersionCode = CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM ProductSkus AS sku
                    WHERE sku.SkuCode = CONCAT(
                        'LEGACY-P',
                        RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
                        '-S01')
                );

                INSERT INTO ProductImages
                (
                    ProductSkuId,
                    Url,
                    AltText,
                    DisplayOrder,
                    IsPrimary
                )
                SELECT
                    sku.Id,
                    LTRIM(RTRIM(product.ImageUrl)),
                    LEFT(product.Name, 200),
                    0,
                    1
                FROM Products AS product
                INNER JOIN ProductVariants AS variant
                    ON variant.ProductId = product.Id
                   AND variant.VersionCode = CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
                INNER JOIN ProductSkus AS sku
                    ON sku.ProductVariantId = variant.Id
                   AND sku.SkuCode = CONCAT(
                        'LEGACY-P',
                        RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
                        '-S01')
                WHERE NULLIF(LTRIM(RTRIM(product.ImageUrl)), N'') IS NOT NULL
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM ProductImages AS image
                      WHERE image.ProductSkuId = sku.Id
                        AND image.IsPrimary = 1
                  );

                IF EXISTS
                (
                    SELECT 1
                    FROM Products AS product
                    LEFT JOIN ProductVariants AS variant
                        ON variant.ProductId = product.Id
                       AND variant.VersionCode = CONCAT('LEGACY-V', CONVERT(varchar(10), product.Id))
                    LEFT JOIN ProductSkus AS sku
                        ON sku.ProductVariantId = variant.Id
                       AND sku.SkuCode = CONCAT(
                           'LEGACY-P',
                           RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
                           '-S01')
                    WHERE variant.Id IS NULL
                       OR sku.Id IS NULL
                       OR sku.Price <> product.Price
                       OR sku.StockQuantity <> product.StockQuantity
                )
                BEGIN
                    THROW 51001, 'Legacy Product to Variant/SKU reconciliation failed. Backfill was rolled back.', 1;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM Specifications AS specification
                    INNER JOIN ProductVariants AS variant
                        ON variant.ProductId = specification.ProductId
                       AND variant.VersionCode = CONCAT(
                           'LEGACY-V',
                           CONVERT(varchar(10), specification.ProductId))
                    LEFT JOIN VariantSpecifications AS variantSpecification
                        ON variantSpecification.ProductVariantId = variant.Id
                    WHERE variantSpecification.ProductVariantId IS NULL
                )
                BEGIN
                    THROW 51002, 'Legacy Specification reconciliation failed. Backfill was rolled back.', 1;
                END;

                IF
                (
                    SELECT COALESCE(SUM(CONVERT(bigint, product.StockQuantity)), 0)
                    FROM Products AS product
                ) <>
                (
                    SELECT COALESCE(SUM(CONVERT(bigint, sku.StockQuantity)), 0)
                    FROM ProductSkus AS sku
                    INNER JOIN ProductVariants AS variant
                        ON variant.Id = sku.ProductVariantId
                    INNER JOIN Products AS product
                        ON product.Id = variant.ProductId
                    WHERE variant.VersionCode = CONCAT(
                              'LEGACY-V',
                              CONVERT(varchar(10), product.Id))
                      AND sku.SkuCode = CONCAT(
                              'LEGACY-P',
                              RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), product.Id), 10),
                              '-S01')
                )
                BEGIN
                    THROW 51003, 'Legacy stock total mismatch. Backfill was rolled back.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET NOCOUNT ON;

                DELETE variant
                FROM ProductVariants AS variant
                INNER JOIN Products AS product
                    ON product.Id = variant.ProductId
                WHERE variant.VersionCode = CONCAT(
                    'LEGACY-V',
                    CONVERT(varchar(10), product.Id));
                """);
        }
    }
}
