using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentAuthenticationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_login_attempts",
                table: "Student",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "Student",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "lockout_end_utc",
                table: "Student",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "failed_login_attempts",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "lockout_end_utc",
                table: "Student");
        }
    }
}
