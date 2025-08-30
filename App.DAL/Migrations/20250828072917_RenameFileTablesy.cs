using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenameFileTablesy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntityImages_Images_FileId",
                table: "EntityImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Images",
                table: "Images");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EntityImages",
                table: "EntityImages");

            migrationBuilder.RenameTable(
                name: "Images",
                newName: "files");

            migrationBuilder.RenameTable(
                name: "EntityImages",
                newName: "entity_files");

            migrationBuilder.RenameIndex(
                name: "IX_EntityImages_FileId",
                table: "entity_files",
                newName: "IX_entity_files_FileId");

            migrationBuilder.RenameIndex(
                name: "IX_EntityImages_EntityType_EntityId",
                table: "entity_files",
                newName: "IX_entity_files_EntityType_EntityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_files",
                table: "files",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_entity_files",
                table: "entity_files",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_entity_files_files_FileId",
                table: "entity_files",
                column: "FileId",
                principalTable: "files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_entity_files_files_FileId",
                table: "entity_files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_files",
                table: "files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_entity_files",
                table: "entity_files");

            migrationBuilder.RenameTable(
                name: "files",
                newName: "Images");

            migrationBuilder.RenameTable(
                name: "entity_files",
                newName: "EntityImages");

            migrationBuilder.RenameIndex(
                name: "IX_entity_files_FileId",
                table: "EntityImages",
                newName: "IX_EntityImages_FileId");

            migrationBuilder.RenameIndex(
                name: "IX_entity_files_EntityType_EntityId",
                table: "EntityImages",
                newName: "IX_EntityImages_EntityType_EntityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Images",
                table: "Images",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EntityImages",
                table: "EntityImages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EntityImages_Images_FileId",
                table: "EntityImages",
                column: "FileId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
