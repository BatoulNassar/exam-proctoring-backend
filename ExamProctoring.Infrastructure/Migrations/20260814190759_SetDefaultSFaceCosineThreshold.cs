using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// Seeds the project default SFace cosine threshold.
    ///
    /// 0.363 is the reference cosine threshold OpenCV publishes for
    /// face_recognition_sface_2021dec, and it is adopted here as the project/demo default. It
    /// is emphatically NOT the legacy SystemSettings.face_match_threshold value of 95: that
    /// column predates the choice of SFace, is not a cosine similarity, and a literal 0.95
    /// would reject every legitimate student, since genuine SFace pairs score roughly 0.4-0.7.
    /// The legacy column is left untouched by this migration.
    ///
    /// Data-only: no schema change. The column itself was added by AddIdentityVerification,
    /// which is already applied and is deliberately not modified here.
    public partial class SetDefaultSFaceCosineThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded on IS NULL so this only ever fills in an unconfigured value. An
            // administrator who has already calibrated a threshold - or who does so before
            // this migration reaches their environment - keeps their number.
            migrationBuilder.Sql(@"
UPDATE [SystemSettings]
SET [sface_cosine_threshold] = 0.363
WHERE [sface_cosine_threshold] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only clears the value this migration could have set. A threshold that has since
            // been calibrated to something else is left alone rather than being discarded by a
            // rollback that was only ever meant to undo a default.
            migrationBuilder.Sql(@"
UPDATE [SystemSettings]
SET [sface_cosine_threshold] = NULL
WHERE [sface_cosine_threshold] = 0.363;");
        }
    }
}
