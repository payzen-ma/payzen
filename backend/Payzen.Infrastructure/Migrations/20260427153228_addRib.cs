using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payzen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addRib : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RibNumber",
                table: "Employees",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RibNumber",
                table: "Employees");
        }
    }
}
