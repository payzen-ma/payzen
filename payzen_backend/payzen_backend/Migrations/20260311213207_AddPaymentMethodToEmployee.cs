using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace payzen_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Employee",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Employee");
        }
    }
}
