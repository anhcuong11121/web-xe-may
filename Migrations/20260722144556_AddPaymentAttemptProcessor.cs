using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAttemptProcessor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProcessedByUserId",
                table: "PaymentAttempts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_ProcessedByUserId",
                table: "PaymentAttempts",
                column: "ProcessedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAttempts_AspNetUsers_ProcessedByUserId",
                table: "PaymentAttempts",
                column: "ProcessedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAttempts_AspNetUsers_ProcessedByUserId",
                table: "PaymentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_ProcessedByUserId",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "PaymentAttempts");
        }
    }
}
