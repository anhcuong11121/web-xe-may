using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class CleanupLegacyProductSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                EXEC(N'
                IF EXISTS (
                    SELECT 1
                    FROM OrderItems AS item
                    LEFT JOIN ProductSkus AS sku ON sku.Id = item.ProductSkuId
                    LEFT JOIN ProductVariants AS variant ON variant.Id = sku.ProductVariantId
                    WHERE sku.Id IS NULL OR variant.ProductId <> item.ProductId
                )
                    THROW 51000, ''Cleanup blocked: OrderItems contain missing or mismatched SKU links.'', 1;

                IF EXISTS (
                    SELECT 1
                    FROM ImportReceiptDetails AS detail
                    LEFT JOIN ProductSkus AS sku ON sku.Id = detail.ProductSkuId
                    LEFT JOIN ProductVariants AS variant ON variant.Id = sku.ProductVariantId
                    WHERE sku.Id IS NULL OR variant.ProductId <> detail.ProductId
                )
                    THROW 51000, ''Cleanup blocked: ImportReceiptDetails contain missing or mismatched SKU links.'', 1;

                IF EXISTS (
                    SELECT 1
                    FROM OrderItems
                    WHERE NULLIF(LTRIM(RTRIM(ProductNameSnapshot)), '''') IS NULL
                       OR NULLIF(LTRIM(RTRIM(VariantNameSnapshot)), '''') IS NULL
                       OR NULLIF(LTRIM(RTRIM(ColorNameSnapshot)), '''') IS NULL
                       OR NULLIF(LTRIM(RTRIM(SkuCodeSnapshot)), '''') IS NULL
                )
                    THROW 51000, ''Cleanup blocked: OrderItem snapshots are incomplete.'', 1;

                IF EXISTS (
                    SELECT 1
                    FROM Products AS product
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM ProductVariants AS variant
                        INNER JOIN ProductSkus AS sku ON sku.ProductVariantId = variant.Id
                        WHERE variant.ProductId = product.Id
                    )
                )
                    THROW 51000, ''Cleanup blocked: at least one Product has no SKU.'', 1;

                IF EXISTS (
                    SELECT 1
                    FROM ProductSkus
                    WHERE Price < 0 OR StockQuantity < 0
                )
                    THROW 51000, ''Cleanup blocked: SKU price or stock is negative.'', 1;

                IF EXISTS (
                    SELECT 1
                    FROM Products AS product
                    OUTER APPLY (
                        SELECT SUM(CAST(sku.StockQuantity AS bigint)) AS SkuStock
                        FROM ProductVariants AS variant
                        INNER JOIN ProductSkus AS sku ON sku.ProductVariantId = variant.Id
                        WHERE variant.ProductId = product.Id
                    ) AS aggregateStock
                    WHERE CAST(product.StockQuantity AS bigint) <> COALESCE(aggregateStock.SkuStock, 0)
                )
                    THROW 51000, ''Cleanup blocked: Product stock and aggregate SKU stock differ.'', 1;

                IF EXISTS (
                    SELECT 1
                    FROM Specifications AS legacySpecification
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM ProductVariants AS variant
                        INNER JOIN VariantSpecifications AS specification
                            ON specification.ProductVariantId = variant.Id
                        WHERE variant.ProductId = legacySpecification.ProductId
                    )
                )
                    THROW 51000, ''Cleanup blocked: a legacy Specification was not migrated.'', 1;

                IF EXISTS (
                    SELECT 1
                    FROM Products AS product
                    WHERE NULLIF(LTRIM(RTRIM(product.ImageUrl)), '''') IS NOT NULL
                      AND NOT EXISTS (
                          SELECT 1
                          FROM ProductVariants AS variant
                          INNER JOIN ProductSkus AS sku ON sku.ProductVariantId = variant.Id
                          INNER JOIN ProductImages AS image ON image.ProductSkuId = sku.Id
                          WHERE variant.ProductId = product.Id
                            AND image.Url = product.ImageUrl
                      )
                )
                    THROW 51000, ''Cleanup blocked: a legacy Product image was not migrated.'', 1;
                ');
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ImportReceiptDetails_Products_ProductId",
                table: "ImportReceiptDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "Specifications");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_ImportReceiptDetails_ProductId",
                table: "ImportReceiptDetails");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ImportReceiptDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "ImportReceiptDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Specifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CurbWeightKg = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Dimensions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EngineCapacityCc = table.Column<int>(type: "int", nullable: false),
                    EngineType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FuelConsumptionLitersPer100Km = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    FuelTankCapacityLiters = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    FuelType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HorsePower = table.Column<int>(type: "int", nullable: false),
                    MaxPower = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OtherDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Specifications_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                UPDATE product
                SET
                    Price = COALESCE(catalog.Price, 0),
                    StockQuantity = COALESCE(catalog.StockQuantity, 0),
                    Color = COALESCE(catalog.ColorName, ''),
                    ImageUrl = catalog.ImageUrl
                FROM Products AS product
                OUTER APPLY (
                    SELECT
                        MIN(sku.Price) AS Price,
                        SUM(sku.StockQuantity) AS StockQuantity,
                        MIN(sku.ColorName) AS ColorName,
                        (
                            SELECT TOP (1) image.Url
                            FROM ProductVariants AS imageVariant
                            INNER JOIN ProductSkus AS imageSku
                                ON imageSku.ProductVariantId = imageVariant.Id
                            INNER JOIN ProductImages AS image
                                ON image.ProductSkuId = imageSku.Id
                            WHERE imageVariant.ProductId = product.Id
                            ORDER BY image.IsPrimary DESC,
                                     image.DisplayOrder,
                                     image.ProductSkuId,
                                     image.Id
                        ) AS ImageUrl
                    FROM ProductVariants AS variant
                    INNER JOIN ProductSkus AS sku ON sku.ProductVariantId = variant.Id
                    WHERE variant.ProductId = product.Id
                ) AS catalog;

                UPDATE item
                SET ProductId = variant.ProductId
                FROM OrderItems AS item
                INNER JOIN ProductSkus AS sku ON sku.Id = item.ProductSkuId
                INNER JOIN ProductVariants AS variant ON variant.Id = sku.ProductVariantId;

                UPDATE detail
                SET ProductId = variant.ProductId
                FROM ImportReceiptDetails AS detail
                INNER JOIN ProductSkus AS sku ON sku.Id = detail.ProductSkuId
                INNER JOIN ProductVariants AS variant ON variant.Id = sku.ProductVariantId;

                INSERT INTO Specifications (
                    ProductId,
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
                    product.Id,
                    selected.EngineType,
                    selected.FuelType,
                    selected.EngineCapacityCc,
                    selected.HorsePower,
                    selected.CurbWeightKg,
                    selected.Dimensions,
                    selected.FuelTankCapacityLiters,
                    selected.MaxPower,
                    selected.FuelConsumptionLitersPer100Km,
                    selected.OtherDetails
                FROM Products AS product
                CROSS APPLY (
                    SELECT TOP (1)
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
                    FROM ProductVariants AS variant
                    INNER JOIN VariantSpecifications AS specification
                        ON specification.ProductVariantId = variant.Id
                    WHERE variant.ProductId = product.Id
                    ORDER BY
                        CASE WHEN variant.VersionCode =
                            CONCAT('LEGACY-V', CONVERT(varchar(20), product.Id))
                            THEN 0 ELSE 1 END,
                        variant.Id
                ) AS selected;

                IF EXISTS (SELECT 1 FROM OrderItems WHERE ProductId IS NULL)
                    THROW 51000, 'Rollback blocked: an OrderItem cannot be mapped back to Product.', 1;

                IF EXISTS (SELECT 1 FROM ImportReceiptDetails WHERE ProductId IS NULL)
                    THROW 51000, 'Rollback blocked: an ImportReceiptDetail cannot be mapped back to Product.', 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "ImportReceiptDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceiptDetails_ProductId",
                table: "ImportReceiptDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Specifications_ProductId",
                table: "Specifications",
                column: "ProductId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportReceiptDetails_Products_ProductId",
                table: "ImportReceiptDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
