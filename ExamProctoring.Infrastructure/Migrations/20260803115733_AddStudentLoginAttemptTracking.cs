using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentLoginAttemptTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentLoginAttempt",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    identifier_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    failed_attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    lockout_end_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentLoginAttempt", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentLoginAttempt_identifier_hash",
                table: "StudentLoginAttempt",
                column: "identifier_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentLoginAttempt");
        }
    }
}
