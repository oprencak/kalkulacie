using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalkulacie.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEditionHiddenColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HiddenColumns",
                table: "CalculationEditions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HiddenColumns",
                table: "CalculationEditions");
        }
    }
}
