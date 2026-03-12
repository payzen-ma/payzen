using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace payzen_backend.Migrations
{
    /// <inheritdoc />
    public partial class Add_BaseSalaryHourly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseSalaryHourly",
                table: "EmployeeSalary",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseSalaryHourly",
                table: "EmployeeSalary");
        }
    }
}
