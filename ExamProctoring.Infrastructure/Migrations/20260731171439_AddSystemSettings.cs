using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    gaze_alert_threshold_sec = table.Column<int>(type: "int", nullable: false),
                    face_sensitivity = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ambient_audio_monitoring = table.Column<bool>(type: "bit", nullable: false),
                    grace_period_minutes = table.Column<int>(type: "int", nullable: false),
                    login_window_minutes = table.Column<int>(type: "int", nullable: false),
                    max_liveness_attempts = table.Column<int>(type: "int", nullable: false),
                    face_match_threshold = table.Column<int>(type: "int", nullable: false),
                    question_randomisation = table.Column<bool>(type: "bit", nullable: false),
                    option_shuffle = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SystemSettings", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
