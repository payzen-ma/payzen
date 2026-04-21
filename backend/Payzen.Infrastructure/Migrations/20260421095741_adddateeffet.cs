using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payzen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class adddateeffet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CategoryChangeDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CimrRatesChangeDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ManagerChangeDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "MaritalStatusChangeDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PrivateInsuranceChangeDate",
                table: "Employees",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryChangeDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CimrRatesChangeDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ManagerChangeDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MaritalStatusChangeDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PrivateInsuranceChangeDate",
                table: "Employees");
        }
    }
}
