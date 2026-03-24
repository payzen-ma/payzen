using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payzen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayHalfToPayrollResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PayrollResults_EmployeeId_Year_Month",
                table: "PayrollResults");

            migrationBuilder.AddColumn<int>(
                name: "PayHalf",
                table: "PayrollResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResults_EmployeeId_Year_Month_PayHalf",
                table: "PayrollResults",
                columns: new[] { "EmployeeId", "Year", "Month", "PayHalf" },
                filter: "[DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PayrollResults_EmployeeId_Year_Month_PayHalf",
                table: "PayrollResults");

            migrationBuilder.DropColumn(
                name: "PayHalf",
                table: "PayrollResults");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResults_EmployeeId_Year_Month",
                table: "PayrollResults",
                columns: new[] { "EmployeeId", "Year", "Month" },
                filter: "[DeletedAt] IS NULL");
        }
    }
}
