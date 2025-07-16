using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditDataToTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_semesters_IsActive",
                table: "semesters");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "topic_versions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "semesters");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "phases");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "workflow_states",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "workflow_states",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "workflow_states",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "workflow_states",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "workflow_states",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "topics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "topics",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "topics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "topics",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "topics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "topic_versions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "topic_versions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "topic_versions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "topic_versions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "topic_versions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "topic_categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "topic_categories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "topic_categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "topic_categories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "topic_categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "semesters",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "semesters",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "semesters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "semesters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "semesters",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "semesters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "reviews",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "review_criteria_scores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "review_criteria_scores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "review_criteria_scores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "review_criteria_scores",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "review_criteria_scores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "review_comments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "review_comments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "review_comments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "review_comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "review_comments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "phases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "phases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "phases",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "phases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "phase_types",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "phase_types",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "phase_types",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "phase_types",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "phase_types",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "lecturer_skills",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "lecturer_skills",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "lecturer_skills",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "lecturer_skills",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "lecturer_skills",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "evaluation_criteria",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "evaluation_criteria",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "evaluation_criteria",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "evaluation_criteria",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "workflow_states");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "workflow_states");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "workflow_states");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "workflow_states");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "workflow_states");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "topic_versions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "topic_versions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "topic_versions");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "topic_versions");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "topic_versions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "topic_categories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "topic_categories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "topic_categories");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "topic_categories");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "topic_categories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "semesters");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "semesters");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "semesters");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "semesters");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "review_criteria_scores");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "review_criteria_scores");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "review_criteria_scores");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "review_criteria_scores");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "review_criteria_scores");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "review_comments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "review_comments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "review_comments");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "review_comments");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "review_comments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "phases");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "phases");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "phases");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "phases");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "phase_types");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "phase_types");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "phase_types");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "phase_types");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "phase_types");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "lecturer_skills");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "lecturer_skills");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "lecturer_skills");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "lecturer_skills");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "lecturer_skills");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "evaluation_criteria");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "evaluation_criteria");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "evaluation_criteria");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "evaluation_criteria");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "topics",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "topic_versions",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "semesters",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "semesters",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "semesters",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "reviews",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "phases",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateIndex(
                name: "IX_semesters_IsActive",
                table: "semesters",
                column: "IsActive");
        }
    }
}
