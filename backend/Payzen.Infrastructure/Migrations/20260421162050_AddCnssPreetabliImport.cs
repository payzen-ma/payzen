using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payzen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCnssPreetabliImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CnssPreetabliImports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AffiliateNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    EmployeeCount = table.Column<int>(type: "int", nullable: false),
                    IssueCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CnssPreetabliImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CnssPreetabliImports_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CnssPreetabliLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CnssPreetabliImportId = table.Column<int>(type: "int", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    AffiliateNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    InsuredNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ChildrenCount = table.Column<int>(type: "int", nullable: false),
                    FamilyAllowanceToPay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FamilyAllowanceToDeduct = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FamilyAllowanceNetToPay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CnssPreetabliLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CnssPreetabliLines_CnssPreetabliImports_CnssPreetabliImportId",
                        column: x => x.CnssPreetabliImportId,
                        principalTable: "CnssPreetabliImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CnssPreetabliImports_CompanyId_Period_CreatedAt",
                table: "CnssPreetabliImports",
                columns: new[] { "CompanyId", "Period", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CnssPreetabliLines_CnssPreetabliImportId_LineNumber",
                table: "CnssPreetabliLines",
                columns: new[] { "CnssPreetabliImportId", "LineNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CnssPreetabliLines");

            migrationBuilder.DropTable(
                name: "CnssPreetabliImports");
        }
    }
}
