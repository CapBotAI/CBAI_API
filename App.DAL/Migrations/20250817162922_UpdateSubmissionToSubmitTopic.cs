using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubmissionToSubmitTopic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_submissions_TopicVersionId_PhaseId_SubmissionRound",
                table: "submissions");

            migrationBuilder.AlterColumn<int>(
                name: "TopicVersionId",
                table: "submissions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "TopicId",
                table: "submissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_submissions_TopicId_PhaseId_SubmissionRound",
                table: "submissions",
                columns: new[] { "TopicId", "PhaseId", "SubmissionRound" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_submissions_TopicVersionId",
                table: "submissions",
                column: "TopicVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_submissions_topics_TopicId",
                table: "submissions",
                column: "TopicId",
                principalTable: "topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_submissions_topics_TopicId",
                table: "submissions");

            migrationBuilder.DropIndex(
                name: "IX_submissions_TopicId_PhaseId_SubmissionRound",
                table: "submissions");

            migrationBuilder.DropIndex(
                name: "IX_submissions_TopicVersionId",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "TopicId",
                table: "submissions");

            migrationBuilder.AlterColumn<int>(
                name: "TopicVersionId",
                table: "submissions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_submissions_TopicVersionId_PhaseId_SubmissionRound",
                table: "submissions",
                columns: new[] { "TopicVersionId", "PhaseId", "SubmissionRound" },
                unique: true);
        }
    }
}
