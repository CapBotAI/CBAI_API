using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldForTopicTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "topics",
                newName: "EN_Title");

            migrationBuilder.AddColumn<string>(
                name: "Abbreviation",
                table: "topics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "topics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Context",
                table: "topics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Problem",
                table: "topics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VN_title",
                table: "topics",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Abbreviation",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "Context",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "Problem",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "VN_title",
                table: "topics");

            migrationBuilder.RenameColumn(
                name: "EN_Title",
                table: "topics",
                newName: "Title");
        }
    }
}
