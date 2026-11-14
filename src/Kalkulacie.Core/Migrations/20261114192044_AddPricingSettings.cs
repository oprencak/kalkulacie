using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalkulacie.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PricingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    AreaMaterialManual = table.Column<decimal>(type: "TEXT", nullable: false),
                    AreaMaterialAutomatic = table.Column<decimal>(type: "TEXT", nullable: false),
                    PieceMaterial = table.Column<decimal>(type: "TEXT", nullable: false),
                    MarginManual = table.Column<decimal>(type: "TEXT", nullable: false),
                    MarginAutomatic = table.Column<decimal>(type: "TEXT", nullable: false),
                    Price1 = table.Column<decimal>(type: "TEXT", nullable: false),
                    Price2 = table.Column<decimal>(type: "TEXT", nullable: false),
                    RoundingDecimals = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PricingSettings",
                columns: new[] { "Id", "AreaMaterialAutomatic", "AreaMaterialManual", "MarginAutomatic", "MarginManual", "PieceMaterial", "Price1", "Price2", "RoundingDecimals" },
                values: new object[] { 1, 1.05m, 1.15m, 1.08m, 1.15m, 1.03m, 1.08m, 1.16m, 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PricingSettings");
        }
    }
}
