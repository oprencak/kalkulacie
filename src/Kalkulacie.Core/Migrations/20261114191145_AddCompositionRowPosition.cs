using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalkulacie.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositionRowPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "CompositionRows",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Existujúce riadky: poradie v rámci výrobku podľa Id (= poradie, v akom vznikli)
            migrationBuilder.Sql("""
                UPDATE CompositionRows
                SET Position = (
                    SELECT COUNT(*) FROM CompositionRows c
                    WHERE c.ProductId = CompositionRows.ProductId AND c.Id <= CompositionRows.Id)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "CompositionRows");
        }
    }
}
