using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorBikeShop.API.Migrations
{
    /// <inheritdoc />
    public partial class UseNoActionForAssignedSupportEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportRequests_AspNetUsers_AssignedEmployeeUserId",
                table: "SupportRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportRequests_AspNetUsers_AssignedEmployeeUserId",
                table: "SupportRequests",
                column: "AssignedEmployeeUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportRequests_AspNetUsers_AssignedEmployeeUserId",
                table: "SupportRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportRequests_AspNetUsers_AssignedEmployeeUserId",
                table: "SupportRequests",
                column: "AssignedEmployeeUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
