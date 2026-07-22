using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsPublishingMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "PublishedAt",
                table: "News",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "News",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "News");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "News",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "News",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Published");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "News");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "News");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "News");

            migrationBuilder.Sql(
                "UPDATE [News] SET [PublishedAt] = SYSUTCDATETIME() WHERE [PublishedAt] IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PublishedAt",
                table: "News",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
