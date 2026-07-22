using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTypeAndAssignedEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedEmployeeUserId",
                table: "SupportRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportType",
                table: "SupportRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "General");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_AssignedEmployeeUserId",
                table: "SupportRequests",
                column: "AssignedEmployeeUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportRequests_AspNetUsers_AssignedEmployeeUserId",
                table: "SupportRequests",
                column: "AssignedEmployeeUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportRequests_AspNetUsers_AssignedEmployeeUserId",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_AssignedEmployeeUserId",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "AssignedEmployeeUserId",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "SupportType",
                table: "SupportRequests");
        }
    }
}
