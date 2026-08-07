using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceCheck",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_session_id = table.Column<int>(type: "int", nullable: false),
                    device_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    checked_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    client_can_proceed = table.Column<bool>(type: "bit", nullable: false),
                    exam_session_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_DeviceCheck", x => x.id);
                    table.ForeignKey(
                        name: "FK_DeviceCheck_StudentSession_student_session_id",
                        column: x => x.student_session_id,
                        principalTable: "StudentSession",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCheckRequirement",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    device_check_id = table.Column<int>(type: "int", nullable: false),
                    requirement_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    detail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_DeviceCheckRequirement", x => x.id);
                    table.ForeignKey(
                        name: "FK_DeviceCheckRequirement_DeviceCheck_device_check_id",
                        column: x => x.device_check_id,
                        principalTable: "DeviceCheck",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCheck_student_session_id_received_at_utc",
                table: "DeviceCheck",
                columns: new[] { "student_session_id", "received_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCheckRequirement_device_check_id",
                table: "DeviceCheckRequirement",
                column: "device_check_id");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCheckRequirement_requirement_id_status",
                table: "DeviceCheckRequirement",
                columns: new[] { "requirement_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceCheckRequirement");

            migrationBuilder.DropTable(
                name: "DeviceCheck");
        }
    }
}
