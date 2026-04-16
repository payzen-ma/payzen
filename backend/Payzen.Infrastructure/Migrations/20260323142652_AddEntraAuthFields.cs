using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payzen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntraAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "AuthType",
                table: "Companies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "JWT"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalId",
                table: "Users",
                column: "ExternalId",
                filter: "[ExternalId] IS NOT NULL AND [DeletedAt] IS NULL"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Users_ExternalId", table: "Users");

            migrationBuilder.DropColumn(name: "ExternalId", table: "Users");

            migrationBuilder.DropColumn(name: "Source", table: "Users");

            migrationBuilder.DropColumn(name: "AuthType", table: "Companies");
        }
    }
}
