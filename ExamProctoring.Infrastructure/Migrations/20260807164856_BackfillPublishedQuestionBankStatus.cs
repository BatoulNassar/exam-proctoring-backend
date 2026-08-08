using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <summary>
    /// Data-only migration: the QuestionBank "Published" status was renamed to
    /// "Locked" in the enum, but rows written before that rename still carry the old
    /// value and would not match any query.
    /// </summary>
    /// <remarks>
    /// This replaces an earlier migration file that carried no [Migration] attribute
    /// and no designer, so Entity Framework never recognised it and it was applied to
    /// no database. A no-op where the old value is already gone.
    /// </remarks>
    public partial class BackfillPublishedQuestionBankStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE QuestionBank SET status = 'Locked' WHERE status = 'Published';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE QuestionBank SET status = 'Published' WHERE status = 'Locked';");
        }
    }
}
