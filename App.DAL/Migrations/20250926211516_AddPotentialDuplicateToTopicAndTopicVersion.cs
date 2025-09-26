using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPotentialDuplicateToTopicAndTopicVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PotentialDuplicate",
                table: "topics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PotentialDuplicate",
                table: "topic_versions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PotentialDuplicate",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "PotentialDuplicate",
                table: "topic_versions");
        }
    }
}
