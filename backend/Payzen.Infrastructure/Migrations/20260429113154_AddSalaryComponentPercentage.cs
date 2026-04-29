using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payzen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryComponentPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Percentage",
                table: "EmployeeSalaryComponents",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Percentage",
                table: "EmployeeSalaryComponents");
        }
    }
}
