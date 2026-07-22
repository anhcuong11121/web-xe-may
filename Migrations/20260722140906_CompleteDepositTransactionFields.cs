using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class CompleteDepositTransactionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Deposits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Deposits",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Completed");

            migrationBuilder.AddColumn<string>(
                name: "TransactionCode",
                table: "Deposits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Deposits] SET [PaidAt] = [DepositDate], " +
                "[TransactionCode] = CONCAT('LEGACY-DEP-', [Id]) " +
                "WHERE [TransactionCode] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionCode",
                table: "Deposits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_TransactionCode",
                table: "Deposits",
                column: "TransactionCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Deposits_TransactionCode",
                table: "Deposits");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Deposits");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Deposits");

            migrationBuilder.DropColumn(
                name: "TransactionCode",
                table: "Deposits");
        }
    }
}
