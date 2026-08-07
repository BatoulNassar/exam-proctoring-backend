using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamProctoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamePublishedToLocked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE QuestionBank SET status = 'Locked' WHERE status = 'Published'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE QuestionBank SET status = 'Published' WHERE status = 'Locked'");
        }
    }
}
