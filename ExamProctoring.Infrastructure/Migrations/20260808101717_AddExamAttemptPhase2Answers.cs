using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamAttemptPhase2Answers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "client_answered_at",
                table: "StudentAnswer",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "duration_ms",
                table: "StudentAnswer",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "IdempotencyRecord",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_session_id = table.Column<int>(type: "int", nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    endpoint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    resource_key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    request_hash = table.Column<byte[]>(type: "varbinary(32)", nullable: false),
                    response_status = table.Column<int>(type: "int", nullable: false),
                    response_body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_IdempotencyRecord", x => x.id);
                    table.ForeignKey(
                        name: "FK_IdempotencyRecord_StudentSession_student_session_id",
                        column: x => x.student_session_id,
                        principalTable: "StudentSession",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_student_session_id_idempotency_key",
                table: "IdempotencyRecord",
                columns: new[] { "student_session_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyRecord");

            migrationBuilder.DropColumn(
                name: "client_answered_at",
                table: "StudentAnswer");

            migrationBuilder.DropColumn(
                name: "duration_ms",
                table: "StudentAnswer");
        }
    }
}
