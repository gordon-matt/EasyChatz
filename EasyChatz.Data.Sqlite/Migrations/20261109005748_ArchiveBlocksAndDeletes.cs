using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyChatz.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveBlocksAndDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "EasyChatz_Rooms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "EasyChatz_Rooms",
                type: "TEXT",
                unicode: false,
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "EasyChatz_Rooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "EasyChatz_Messages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "EasyChatz_Messages",
                type: "TEXT",
                unicode: false,
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EasyChatz_Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EasyChatz_UserBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BlockerUserId = table.Column<string>(type: "TEXT", unicode: false, maxLength: 128, nullable: false),
                    BlockedUserId = table.Column<string>(type: "TEXT", unicode: false, maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EasyChatz_UserBlocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_UserBlocks_BlockedUserId",
                table: "EasyChatz_UserBlocks",
                column: "BlockedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_UserBlocks_BlockerUserId_BlockedUserId",
                table: "EasyChatz_UserBlocks",
                columns: new[] { "BlockerUserId", "BlockedUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EasyChatz_UserBlocks");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "EasyChatz_Rooms");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "EasyChatz_Rooms");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "EasyChatz_Rooms");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "EasyChatz_Messages");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EasyChatz_Messages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EasyChatz_Messages");
        }
    }
}
