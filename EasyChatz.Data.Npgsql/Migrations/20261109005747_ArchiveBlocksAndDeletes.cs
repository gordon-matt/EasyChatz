using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EasyChatz.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveBlocksAndDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                schema: "chatz",
                table: "Rooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                schema: "chatz",
                table: "Rooms",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "chatz",
                table: "Rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "chatz",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                schema: "chatz",
                table: "Messages",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "chatz",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserBlocks",
                schema: "chatz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BlockerUserId = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    BlockedUserId = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockedUserId",
                schema: "chatz",
                table: "UserBlocks",
                column: "BlockedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerUserId_BlockedUserId",
                schema: "chatz",
                table: "UserBlocks",
                columns: new[] { "BlockerUserId", "BlockedUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBlocks",
                schema: "chatz");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                schema: "chatz",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                schema: "chatz",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "chatz",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "chatz",
                table: "Messages");
        }
    }
}
