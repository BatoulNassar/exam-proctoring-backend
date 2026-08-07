using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxWarningsBeforeTermination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfilled with 3 rather than the generated 0: zero means "never
            // terminate automatically", so an existing settings row would silently
            // opt out of the limit the feature was added for.
            migrationBuilder.AddColumn<int>(
                name: "max_warnings_before_termination",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_warnings_before_termination",
                table: "SystemSettings");
        }
    }
}
