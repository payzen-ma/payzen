using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payzen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollTaxSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollTaxSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollResultId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CumulBrut = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CumulCnss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CumulAmo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CumulSni = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CumulIr = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CumulNet = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TauxEffectif = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollTaxSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollTaxSnapshots_PayrollResults_PayrollResultId",
                        column: x => x.PayrollResultId,
                        principalTable: "PayrollResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollTaxSnapshots_EmployeeId_CompanyId_Year_Month",
                table: "PayrollTaxSnapshots",
                columns: new[] { "EmployeeId", "CompanyId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollTaxSnapshots_PayrollResultId",
                table: "PayrollTaxSnapshots",
                column: "PayrollResultId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollTaxSnapshots");
        }
    }
}
