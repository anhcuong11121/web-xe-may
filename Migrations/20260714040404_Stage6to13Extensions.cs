using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class Stage6to13Extensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Deposits_OrderId",
                table: "Deposits");

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAt",
                table: "SupportRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Response",
                table: "SupportRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessedByUserId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ImportReceipts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProcessedByUserId",
                table: "Orders",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceipts_CreatedByUserId",
                table: "ImportReceipts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_OrderId",
                table: "Deposits",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportReceipts_AspNetUsers_CreatedByUserId",
                table: "ImportReceipts",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_ProcessedByUserId",
                table: "Orders",
                column: "ProcessedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportReceipts_AspNetUsers_CreatedByUserId",
                table: "ImportReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_ProcessedByUserId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ProcessedByUserId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_ImportReceipts_CreatedByUserId",
                table: "ImportReceipts");

            migrationBuilder.DropIndex(
                name: "IX_Deposits_OrderId",
                table: "Deposits");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "Response",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ImportReceipts");

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_OrderId",
                table: "Deposits",
                column: "OrderId");
        }
    }
}
