using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    VersionCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.CheckConstraint("CK_ProductVariants_Status", "[Status] IN ('Active', 'Inactive', 'Discontinued')");
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSkus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    SkuCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ColorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ColorHexCode = table.Column<string>(type: "varchar(9)", unicode: false, maxLength: 9, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Active"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSkus", x => x.Id);
                    table.CheckConstraint("CK_ProductSkus_Price_NonNegative", "[Price] >= 0");
                    table.CheckConstraint("CK_ProductSkus_Status", "[Status] IN ('Active', 'Inactive', 'Discontinued')");
                    table.CheckConstraint("CK_ProductSkus_StockQuantity_NonNegative", "[StockQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_ProductSkus_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariantSpecifications",
                columns: table => new
                {
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    EngineType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FuelType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EngineCapacityCc = table.Column<int>(type: "int", nullable: false),
                    HorsePower = table.Column<int>(type: "int", nullable: false),
                    CurbWeightKg = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Dimensions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FuelTankCapacityLiters = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    MaxPower = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FuelConsumptionLitersPer100Km = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    OtherDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantSpecifications", x => x.ProductVariantId);
                    table.ForeignKey(
                        name: "FK_VariantSpecifications_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductSkuId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.CheckConstraint("CK_ProductImages_DisplayOrder_NonNegative", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ProductImages_ProductSkus_ProductSkuId",
                        column: x => x.ProductSkuId,
                        principalTable: "ProductSkus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductSkuId",
                table: "ProductImages",
                column: "ProductSkuId",
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductSkuId_DisplayOrder",
                table: "ProductImages",
                columns: new[] { "ProductSkuId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSkus_ProductVariantId_ColorName",
                table: "ProductSkus",
                columns: new[] { "ProductVariantId", "ColorName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSkus_ProductVariantId_Status",
                table: "ProductSkus",
                columns: new[] { "ProductVariantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSkus_SkuCode",
                table: "ProductSkus",
                column: "SkuCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_Status",
                table: "ProductVariants",
                columns: new[] { "ProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_VersionCode",
                table: "ProductVariants",
                columns: new[] { "ProductId", "VersionCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "VariantSpecifications");

            migrationBuilder.DropTable(
                name: "ProductSkus");

            migrationBuilder.DropTable(
                name: "ProductVariants");
        }
    }
}
