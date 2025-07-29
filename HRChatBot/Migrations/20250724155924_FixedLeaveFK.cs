using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRChatBot.Migrations
{
    /// <inheritdoc />
    public partial class FixedLeaveFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leaves_Employees_EmployeeEmpId",
                table: "Leaves");

            migrationBuilder.DropIndex(
                name: "IX_Leaves_EmployeeEmpId",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "EmployeeEmpId",
                table: "Leaves");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_EmpId",
                table: "Leaves",
                column: "EmpId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leaves_Employees_EmpId",
                table: "Leaves",
                column: "EmpId",
                principalTable: "Employees",
                principalColumn: "EmpId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leaves_Employees_EmpId",
                table: "Leaves");

            migrationBuilder.DropIndex(
                name: "IX_Leaves_EmpId",
                table: "Leaves");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeEmpId",
                table: "Leaves",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_EmployeeEmpId",
                table: "Leaves",
                column: "EmployeeEmpId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leaves_Employees_EmployeeEmpId",
                table: "Leaves",
                column: "EmployeeEmpId",
                principalTable: "Employees",
                principalColumn: "EmpId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
