using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionSkuLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductSkuId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductSkuId",
                table: "ImportReceiptDetails",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                SET NOCOUNT ON;

                UPDATE item
                SET item.ProductSkuId = sku.Id
                FROM OrderItems AS item
                INNER JOIN ProductVariants AS variant
                    ON variant.ProductId = item.ProductId
                   AND variant.VersionCode = CONCAT(
                       'LEGACY-V',
                       CONVERT(varchar(10), item.ProductId))
                INNER JOIN ProductSkus AS sku
                    ON sku.ProductVariantId = variant.Id
                   AND sku.SkuCode = CONCAT(
                       'LEGACY-P',
                       RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), item.ProductId), 10),
                       '-S01')
                WHERE item.ProductSkuId IS NULL;

                UPDATE detail
                SET detail.ProductSkuId = sku.Id
                FROM ImportReceiptDetails AS detail
                INNER JOIN ProductVariants AS variant
                    ON variant.ProductId = detail.ProductId
                   AND variant.VersionCode = CONCAT(
                       'LEGACY-V',
                       CONVERT(varchar(10), detail.ProductId))
                INNER JOIN ProductSkus AS sku
                    ON sku.ProductVariantId = variant.Id
                   AND sku.SkuCode = CONCAT(
                       'LEGACY-P',
                       RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), detail.ProductId), 10),
                       '-S01')
                WHERE detail.ProductSkuId IS NULL;

                IF EXISTS
                (
                    SELECT 1
                    FROM OrderItems AS item
                    LEFT JOIN ProductSkus AS sku
                        ON sku.Id = item.ProductSkuId
                    LEFT JOIN ProductVariants AS variant
                        ON variant.Id = sku.ProductVariantId
                    WHERE item.ProductSkuId IS NULL
                       OR variant.ProductId <> item.ProductId
                )
                BEGIN
                    THROW 51010, 'OrderItem legacy SKU reconciliation failed. Migration was rolled back.', 1;
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
                       OR variant.ProductId <> detail.ProductId
                )
                BEGIN
                    THROW 51011, 'ImportReceiptDetail legacy SKU reconciliation failed. Migration was rolled back.', 1;
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductSkuId",
                table: "OrderItems",
                column: "ProductSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceiptDetails_ProductSkuId",
                table: "ImportReceiptDetails",
                column: "ProductSkuId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportReceiptDetails_ProductSkus_ProductSkuId",
                table: "ImportReceiptDetails",
                column: "ProductSkuId",
                principalTable: "ProductSkus",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_ProductSkus_ProductSkuId",
                table: "OrderItems",
                column: "ProductSkuId",
                principalTable: "ProductSkus",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportReceiptDetails_ProductSkus_ProductSkuId",
                table: "ImportReceiptDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_ProductSkus_ProductSkuId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductSkuId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_ImportReceiptDetails_ProductSkuId",
                table: "ImportReceiptDetails");

            migrationBuilder.DropColumn(
                name: "ProductSkuId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductSkuId",
                table: "ImportReceiptDetails");
        }
    }
}
