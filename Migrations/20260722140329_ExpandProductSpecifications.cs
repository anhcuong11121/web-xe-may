using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class ExpandProductSpecifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurbWeightKg",
                table: "Specifications",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dimensions",
                table: "Specifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FuelConsumptionLitersPer100Km",
                table: "Specifications",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FuelTankCapacityLiters",
                table: "Specifications",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaxPower",
                table: "Specifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherDetails",
                table: "Specifications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurbWeightKg",
                table: "Specifications");

            migrationBuilder.DropColumn(
                name: "Dimensions",
                table: "Specifications");

            migrationBuilder.DropColumn(
                name: "FuelConsumptionLitersPer100Km",
                table: "Specifications");

            migrationBuilder.DropColumn(
                name: "FuelTankCapacityLiters",
                table: "Specifications");

            migrationBuilder.DropColumn(
                name: "MaxPower",
                table: "Specifications");

            migrationBuilder.DropColumn(
                name: "OtherDetails",
                table: "Specifications");
        }
    }
}
