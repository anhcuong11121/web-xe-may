using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOrderItemIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dữ liệu cũ (nếu có) được gộp trước khi áp unique index.
            migrationBuilder.Sql(
                """
                ;WITH Aggregated AS
                (
                    SELECT [OrderId], [ProductId], MIN([Id]) AS [KeepId], SUM([Quantity]) AS [TotalQuantity]
                    FROM [OrderItems]
                    GROUP BY [OrderId], [ProductId]
                    HAVING COUNT(*) > 1
                )
                UPDATE target
                SET target.[Quantity] = aggregated.[TotalQuantity]
                FROM [OrderItems] target
                INNER JOIN Aggregated aggregated ON target.[Id] = aggregated.[KeepId];

                ;WITH Ranked AS
                (
                    SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [OrderId], [ProductId] ORDER BY [Id]) AS [RowNumber]
                    FROM [OrderItems]
                )
                DELETE target
                FROM [OrderItems] target
                INNER JOIN Ranked ranked ON target.[Id] = ranked.[Id]
                WHERE ranked.[RowNumber] > 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_TotalAmount_NonNegative",
                table: "Orders",
                sql: "[TotalAmount] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ProductId",
                table: "OrderItems",
                columns: new[] { "OrderId", "ProductId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_Quantity_Positive",
                table: "OrderItems",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_UnitPrice_NonNegative",
                table: "OrderItems",
                sql: "[UnitPrice] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_TotalAmount_NonNegative",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId_ProductId",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_Quantity_Positive",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_UnitPrice_NonNegative",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");
        }
    }
}
