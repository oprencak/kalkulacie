using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalkulacie.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExtraPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price1",
                table: "PricingSettings");

            migrationBuilder.DropColumn(
                name: "Price2",
                table: "PricingSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price1",
                table: "PricingSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Price2",
                table: "PricingSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "PricingSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Price1", "Price2" },
                values: new object[] { 1.08m, 1.16m });
        }
    }
}
