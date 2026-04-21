using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Payzen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemConstants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IrTaxBrackets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    Deduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IrTaxBrackets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConstants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConstants", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "IrTaxBrackets",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Deduction", "DeletedAt", "DeletedBy", "EffectiveDate", "MaxIncome", "MinIncome", "Rate", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 0.00m, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3333.33m, 0m, 0.00m, null, null },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 333.33m, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5000.00m, 3333.34m, 0.10m, null, null },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 833.33m, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6666.67m, 5000.01m, 0.20m, null, null },
                    { 4, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 1500.00m, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8333.33m, 6666.68m, 0.30m, null, null },
                    { 5, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 1833.33m, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 15000.00m, 8333.34m, 0.34m, null, null },
                    { 6, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 2283.33m, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 999999999.99m, 15000.01m, 0.37m, null, null }
                });

            migrationBuilder.InsertData(
                table: "SystemConstants",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "EffectiveDate", "Key", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Jours de travail référentiels", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "WorkDaysRef", null, null, 26.00m },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Heures de travail référentielles", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "WorkHoursRef", null, null, 191.00m },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "SMIG Horaire (MAD)", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SmigHoraire", null, null, 17.10m },
                    { 4, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond CNSS Mensuel", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondCnssMensuel", null, null, 6000.00m },
                    { 5, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Taux CNSS RG Salarial", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CnssRgSalarial", null, null, 0.0448m },
                    { 6, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Taux CNSS RG Patronal", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CnssRgPatronal", null, null, 0.0898m },
                    { 7, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Taux CNSS AMO Salarial", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CnssAmoSalarial", null, null, 0.0226m },
                    { 8, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Taux CNSS AMO Patronal", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CnssAmoPatronal", null, null, 0.0226m },
                    { 9, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Taux CNSS AMO Participation Patronale", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CnssAmoParticipPatronal", null, null, 0.0185m },
                    { 10, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Taux CNSS Allocations Familiales Patronal", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CnssAllocFamPatronal", null, null, 0.0640m },
                    { 11, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Taux CNSS FP Patronal", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CnssFpPatronal", null, null, 0.0160m },
                    { 12, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Transport", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiTransport", null, null, 500.00m },
                    { 13, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Transport Hors Urbain", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiTransportHu", null, null, 750.00m },
                    { 14, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Tournée", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiTournee", null, null, 1500.00m },
                    { 15, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Représentation (Taux)", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiRepresentation", null, null, 0.10m },
                    { 16, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Panier/Jour", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiPanierJour", null, null, 34.20m },
                    { 17, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Caisse", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiCaisse", null, null, 239.00m },
                    { 18, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Caisse DGI", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiCaisseDgi", null, null, 190.00m },
                    { 19, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Lait", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiLait", null, null, 196.00m },
                    { 20, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Lait DGI", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiLaitDgi", null, null, 150.00m },
                    { 21, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Outillage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiOutillage", null, null, 119.00m },
                    { 22, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Outillage DGI", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiOutillageDgi", null, null, 100.00m },
                    { 23, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Salissure", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiSalissure", null, null, 239.00m },
                    { 24, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Salissure DGI", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiSalissureDgi", null, null, 210.00m },
                    { 25, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Gratif Annuelle", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiGratifAnnuel", null, null, 5000.00m },
                    { 26, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Plafond NI Gratif DGI", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PlafondNiGratifDgi", null, null, 2500.00m },
                    { 27, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Déduction Famille par enfant (IR)", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "IrDeductionFamille", null, null, 30.00m },
                    { 28, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Frais Pro: Seuil 35%", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "FpSeuil35", null, null, 6500.00m },
                    { 29, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Frais Pro: Taux <= Seuil", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "FpTaux35", null, null, 0.35m },
                    { 30, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Frais Pro: Plafond <= Seuil", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "FpPlafond35", null, null, 2916.67m },
                    { 31, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Frais Pro: Taux > Seuil", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "FpTaux25", null, null, 0.25m },
                    { 32, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, null, "Frais Pro: Plafond > Seuil", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "FpPlafond25", null, null, 2916.67m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemConstants_Key_EffectiveDate",
                table: "SystemConstants",
                columns: new[] { "Key", "EffectiveDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IrTaxBrackets");

            migrationBuilder.DropTable(
                name: "SystemConstants");
        }
    }
}
