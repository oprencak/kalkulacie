using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalkulacie.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculationEditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalculationEditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationEditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalculationEditionProducts",
                columns: table => new
                {
                    CalculationEditionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", nullable: false),
                    Calculation = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationEditionProducts", x => new { x.CalculationEditionId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_CalculationEditionProducts_CalculationEditions_CalculationEditionId",
                        column: x => x.CalculationEditionId,
                        principalTable: "CalculationEditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalculationEditionProducts");

            migrationBuilder.DropTable(
                name: "CalculationEditions");
        }
    }
}
