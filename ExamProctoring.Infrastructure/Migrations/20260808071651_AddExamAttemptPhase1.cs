using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamAttemptPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "device_id",
                table: "StudentSession",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ends_at",
                table: "StudentSession",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                table: "StudentSession",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<int>(
                name: "question_count",
                table: "StudentSession",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "started_at",
                table: "StudentSession",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttemptQuestion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_session_id = table.Column<int>(type: "int", nullable: false),
                    question_id = table.Column<int>(type: "int", nullable: false),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    stem = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    marks = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AttemptQuestion", x => x.id);
                    table.ForeignKey(
                        name: "FK_AttemptQuestion_Question_question_id",
                        column: x => x.question_id,
                        principalTable: "Question",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttemptQuestion_StudentSession_student_session_id",
                        column: x => x.student_session_id,
                        principalTable: "StudentSession",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttemptQuestionOption",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    attempt_question_id = table.Column<int>(type: "int", nullable: false),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    source_slot = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    label = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_AttemptQuestionOption", x => x.id);
                    table.ForeignKey(
                        name: "FK_AttemptQuestionOption_AttemptQuestion_attempt_question_id",
                        column: x => x.attempt_question_id,
                        principalTable: "AttemptQuestion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentSession_public_id",
                table: "StudentSession",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestion_question_id",
                table: "AttemptQuestion",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestion_student_session_id_ordinal",
                table: "AttemptQuestion",
                columns: new[] { "student_session_id", "ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestion_student_session_id_public_id",
                table: "AttemptQuestion",
                columns: new[] { "student_session_id", "public_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestion_student_session_id_question_id",
                table: "AttemptQuestion",
                columns: new[] { "student_session_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestionOption_attempt_question_id_ordinal",
                table: "AttemptQuestionOption",
                columns: new[] { "attempt_question_id", "ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestionOption_attempt_question_id_public_id",
                table: "AttemptQuestionOption",
                columns: new[] { "attempt_question_id", "public_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttemptQuestionOption");

            migrationBuilder.DropTable(
                name: "AttemptQuestion");

            migrationBuilder.DropIndex(
                name: "IX_StudentSession_public_id",
                table: "StudentSession");

            migrationBuilder.DropColumn(
                name: "device_id",
                table: "StudentSession");

            migrationBuilder.DropColumn(
                name: "ends_at",
                table: "StudentSession");

            migrationBuilder.DropColumn(
                name: "public_id",
                table: "StudentSession");

            migrationBuilder.DropColumn(
                name: "question_count",
                table: "StudentSession");

            migrationBuilder.DropColumn(
                name: "started_at",
                table: "StudentSession");
        }
    }
}
