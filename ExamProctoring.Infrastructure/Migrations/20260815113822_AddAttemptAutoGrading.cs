using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptAutoGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AutoScore_student_session_id",
                table: "AutoScore");

            migrationBuilder.AddColumn<DateTime>(
                name: "graded_at_utc",
                table: "StudentSession",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoScore_student_session_id_question_id",
                table: "AutoScore",
                columns: new[] { "student_session_id", "question_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AutoScore_student_session_id_question_id",
                table: "AutoScore");

            migrationBuilder.DropColumn(
                name: "graded_at_utc",
                table: "StudentSession");

            migrationBuilder.CreateIndex(
                name: "IX_AutoScore_student_session_id",
                table: "AutoScore",
                column: "student_session_id");
        }
    }
}
