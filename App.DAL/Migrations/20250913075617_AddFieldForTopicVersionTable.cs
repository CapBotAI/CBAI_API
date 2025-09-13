using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldForTopicVersionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "topic_versions",
                newName: "EN_Title");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "topic_versions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Context",
                table: "topic_versions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Problem",
                table: "topic_versions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VN_title",
                table: "topic_versions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "topic_versions");

            migrationBuilder.DropColumn(
                name: "Context",
                table: "topic_versions");

            migrationBuilder.DropColumn(
                name: "Problem",
                table: "topic_versions");

            migrationBuilder.DropColumn(
                name: "VN_title",
                table: "topic_versions");

            migrationBuilder.RenameColumn(
                name: "EN_Title",
                table: "topic_versions",
                newName: "Title");
        }
    }
}
