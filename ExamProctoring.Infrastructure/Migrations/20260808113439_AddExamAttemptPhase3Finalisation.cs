using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamAttemptPhase3Finalisation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "answered_count",
                table: "StudentSession",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "finalisation_reason",
                table: "StudentSession",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "finalised_at",
                table: "StudentSession",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "receipt_code",
                table: "StudentSession",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentSession_receipt_code",
                table: "StudentSession",
                column: "receipt_code",
                unique: true,
                filter: "[receipt_code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSession_status_ends_at",
                table: "StudentSession",
                columns: new[] { "status", "ends_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentSession_receipt_code",
                table: "StudentSession");

            migrationBuilder.DropIndex(
                name: "IX_StudentSession_status_ends_at",
                table: "StudentSession");

            migrationBuilder.DropColumn(
                name: "answered_count",
                table: "StudentSession");

            migrationBuilder.DropColumn(
                name: "finalisation_reason",
                table: "StudentSession");

            migrationBuilder.DropColumn(
                name: "finalised_at",
                table: "StudentSession");

            migrationBuilder.DropColumn(
                name: "receipt_code",
                table: "StudentSession");
        }
    }
}
