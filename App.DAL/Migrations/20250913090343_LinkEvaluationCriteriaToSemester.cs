using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class LinkEvaluationCriteriaToSemester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SemesterId",
                table: "evaluation_criteria",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_criteria_SemesterId",
                table: "evaluation_criteria",
                column: "SemesterId");

            migrationBuilder.AddForeignKey(
                name: "FK_evaluation_criteria_semesters_SemesterId",
                table: "evaluation_criteria",
                column: "SemesterId",
                principalTable: "semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_evaluation_criteria_semesters_SemesterId",
                table: "evaluation_criteria");

            migrationBuilder.DropIndex(
                name: "IX_evaluation_criteria_SemesterId",
                table: "evaluation_criteria");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "evaluation_criteria");
        }
    }
}