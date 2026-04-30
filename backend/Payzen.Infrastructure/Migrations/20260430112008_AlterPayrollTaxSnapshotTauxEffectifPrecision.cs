using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payzen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlterPayrollTaxSnapshotTauxEffectifPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TauxEffectif",
                table: "PayrollTaxSnapshots",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TauxEffectif",
                table: "PayrollTaxSnapshots",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}
