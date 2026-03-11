using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace payzen_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCnss_Cimr_Base : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmoBase",
                table: "PayrollResults",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CimrBase",
                table: "PayrollResults",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CnssBase",
                table: "PayrollResults",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MutuelleBase",
                table: "PayrollResults",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmoBase",
                table: "PayrollResults");

            migrationBuilder.DropColumn(
                name: "CimrBase",
                table: "PayrollResults");

            migrationBuilder.DropColumn(
                name: "CnssBase",
                table: "PayrollResults");

            migrationBuilder.DropColumn(
                name: "MutuelleBase",
                table: "PayrollResults");
        }
    }
}
