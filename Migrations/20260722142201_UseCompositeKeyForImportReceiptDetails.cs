using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class UseCompositeKeyForImportReceiptDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF EXISTS (" +
                "SELECT 1 FROM [ImportReceiptDetails] " +
                "GROUP BY [ImportReceiptId], [ProductId] HAVING COUNT(*) > 1) " +
                "THROW 51000, 'ImportReceiptDetails contains duplicate ImportReceiptId/ProductId pairs.', 1;");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportReceiptDetails",
                table: "ImportReceiptDetails");

            migrationBuilder.DropIndex(
                name: "IX_ImportReceiptDetails_ImportReceiptId",
                table: "ImportReceiptDetails");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ImportReceiptDetails");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportReceiptDetails",
                table: "ImportReceiptDetails",
                columns: new[] { "ImportReceiptId", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportReceiptDetails",
                table: "ImportReceiptDetails");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ImportReceiptDetails",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportReceiptDetails",
                table: "ImportReceiptDetails",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceiptDetails_ImportReceiptId",
                table: "ImportReceiptDetails",
                column: "ImportReceiptId");
        }
    }
}
