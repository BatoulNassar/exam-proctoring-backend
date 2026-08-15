using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "sface_cosine_threshold",
                table: "SystemSettings",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "reference_face_embedding",
                table: "Student",
                type: "varbinary(512)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reference_face_generated_at",
                table: "Student",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_face_model",
                table: "Student",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_face_model_version",
                table: "Student",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IdentityVerificationSession",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_session_id = table.Column<int>(type: "int", nullable: false),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    attempts_used = table.Column<int>(type: "int", nullable: false),
                    max_attempts = table.Column<int>(type: "int", nullable: false),
                    verified_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    device_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
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
                    table.PrimaryKey("PK_IdentityVerificationSession", x => x.id);
                    table.ForeignKey(
                        name: "FK_IdentityVerificationSession_StudentSession_student_session_id",
                        column: x => x.student_session_id,
                        principalTable: "StudentSession",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityVerificationAttempt",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    identity_verification_session_id = table.Column<int>(type: "int", nullable: false),
                    client_attempt_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    attempt_number = table.Column<int>(type: "int", nullable: false),
                    attempts_remaining_after = table.Column<int>(type: "int", nullable: false),
                    match_score = table.Column<double>(type: "float", nullable: true),
                    threshold_used = table.Column<double>(type: "float", nullable: true),
                    liveness_accepted = table.Column<bool>(type: "bit", nullable: false),
                    liveness_blink_count = table.Column<int>(type: "int", nullable: false),
                    liveness_frames_analysed = table.Column<int>(type: "int", nullable: false),
                    liveness_duration_ms = table.Column<int>(type: "int", nullable: false),
                    liveness_min_eye_openness = table.Column<double>(type: "float", nullable: false),
                    liveness_max_eye_openness = table.Column<double>(type: "float", nullable: false),
                    liveness_rejection_reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    embedding_model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    embedding_model_version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    captured_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    attempted_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_IdentityVerificationAttempt", x => x.id);
                    table.ForeignKey(
                        name: "FK_IdentityVerificationAttempt_IdentityVerificationSession_identity_verification_session_id",
                        column: x => x.identity_verification_session_id,
                        principalTable: "IdentityVerificationSession",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Student_reference_face_embedding_length",
                table: "Student",
                sql: "[reference_face_embedding] IS NULL OR DATALENGTH([reference_face_embedding]) = 512");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityVerificationAttempt_identity_verification_session_id_attempted_at_utc",
                table: "IdentityVerificationAttempt",
                columns: new[] { "identity_verification_session_id", "attempted_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityVerificationAttempt_identity_verification_session_id_client_attempt_id",
                table: "IdentityVerificationAttempt",
                columns: new[] { "identity_verification_session_id", "client_attempt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityVerificationSession_public_id",
                table: "IdentityVerificationSession",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityVerificationSession_student_session_id",
                table: "IdentityVerificationSession",
                column: "student_session_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityVerificationAttempt");

            migrationBuilder.DropTable(
                name: "IdentityVerificationSession");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Student_reference_face_embedding_length",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "sface_cosine_threshold",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "reference_face_embedding",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "reference_face_generated_at",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "reference_face_model",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "reference_face_model_version",
                table: "Student");
        }
    }
}
