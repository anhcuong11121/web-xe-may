using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class CutoverTransactionsToSku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId_ProductId",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportReceiptDetails",
                table: "ImportReceiptDetails");

            migrationBuilder.AddColumn<string>(
                name: "ColorNameSnapshot",
                table: "OrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductNameSnapshot",
                table: "OrderItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkuCodeSnapshot",
                table: "OrderItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantNameSnapshot",
                table: "OrderItems",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.Sql(
                """
                SET NOCOUNT ON;

                IF EXISTS
                (
                    SELECT 1
                    FROM OrderItems AS item
                    LEFT JOIN ProductSkus AS sku
                        ON sku.Id = item.ProductSkuId
                    LEFT JOIN ProductVariants AS variant
                        ON variant.Id = sku.ProductVariantId
                    WHERE item.ProductSkuId IS NULL
                       OR sku.Id IS NULL
                       OR variant.ProductId <> item.ProductId
                )
                BEGIN
                    THROW 51020, 'OrderItem SKU cutover validation failed. Migration was rolled back.', 1;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM ImportReceiptDetails AS detail
                    LEFT JOIN ProductSkus AS sku
                        ON sku.Id = detail.ProductSkuId
                    LEFT JOIN ProductVariants AS variant
                        ON variant.Id = sku.ProductVariantId
                    WHERE detail.ProductSkuId IS NULL
                       OR sku.Id IS NULL
                       OR variant.ProductId <> detail.ProductId
                )
                BEGIN
                    THROW 51021, 'ImportReceiptDetail SKU cutover validation failed. Migration was rolled back.', 1;
                END;

                UPDATE item
                SET
                    item.ProductNameSnapshot = product.Name,
                    item.VariantNameSnapshot = variant.Name,
                    item.ColorNameSnapshot = sku.ColorName,
                    item.SkuCodeSnapshot = sku.SkuCode
                FROM OrderItems AS item
                INNER JOIN ProductSkus AS sku
                    ON sku.Id = item.ProductSkuId
                INNER JOIN ProductVariants AS variant
                    ON variant.Id = sku.ProductVariantId
                INNER JOIN Products AS product
                    ON product.Id = variant.ProductId;

                IF EXISTS
                (
                    SELECT 1
                    FROM OrderItems AS item
                    WHERE NULLIF(LTRIM(RTRIM(item.ProductNameSnapshot)), N'') IS NULL
                       OR NULLIF(LTRIM(RTRIM(item.VariantNameSnapshot)), N'') IS NULL
                       OR NULLIF(LTRIM(RTRIM(item.ColorNameSnapshot)), N'') IS NULL
                       OR NULLIF(LTRIM(RTRIM(item.SkuCodeSnapshot)), N'') IS NULL
                )
                BEGIN
                    THROW 51022, 'OrderItem snapshot backfill failed. Migration was rolled back.', 1;
                END;

                IF EXISTS
                (
                    SELECT item.OrderId, item.ProductSkuId
                    FROM OrderItems AS item
                    GROUP BY item.OrderId, item.ProductSkuId
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51023, 'Duplicate OrderId/ProductSkuId detected. Migration was rolled back.', 1;
                END;

                IF EXISTS
                (
                    SELECT detail.ImportReceiptId, detail.ProductSkuId
                    FROM ImportReceiptDetails AS detail
                    GROUP BY detail.ImportReceiptId, detail.ProductSkuId
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51024, 'Duplicate ImportReceiptId/ProductSkuId detected. Migration was rolled back.', 1;
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ProductSkuId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ColorNameSnapshot",
                table: "OrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductNameSnapshot",
                table: "OrderItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SkuCodeSnapshot",
                table: "OrderItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VariantNameSnapshot",
                table: "OrderItems",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductSkuId",
                table: "ImportReceiptDetails",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportReceiptDetails",
                table: "ImportReceiptDetails",
                columns: new[] { "ImportReceiptId", "ProductSkuId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ProductSkuId",
                table: "OrderItems",
                columns: new[] { "OrderId", "ProductSkuId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId_ProductSkuId",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportReceiptDetails",
                table: "ImportReceiptDetails");

            migrationBuilder.DropColumn(
                name: "ColorNameSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductNameSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SkuCodeSnapshot",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VariantNameSnapshot",
                table: "OrderItems");

            migrationBuilder.AlterColumn<int>(
                name: "ProductSkuId",
                table: "OrderItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ProductSkuId",
                table: "ImportReceiptDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportReceiptDetails",
                table: "ImportReceiptDetails",
                columns: new[] { "ImportReceiptId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ProductId",
                table: "OrderItems",
                columns: new[] { "OrderId", "ProductId" },
                unique: true);
        }
    }
}
