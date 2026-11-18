using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyChatz.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EasyChatz_Connections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequesterUserId = table.Column<string>(type: "TEXT", unicode: false, maxLength: 128, nullable: false),
                    RecipientUserId = table.Column<string>(type: "TEXT", unicode: false, maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Introduction = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EasyChatz_Connections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Connections_RecipientUserId",
                table: "EasyChatz_Connections",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Connections_RequesterUserId_RecipientUserId",
                table: "EasyChatz_Connections",
                columns: new[] { "RequesterUserId", "RecipientUserId" },
                unique: true);

            // People who already exchanged a direct message are connected, so turning
            // RequireConnections on later does not separate them.
            migrationBuilder.Sql(
                """
                INSERT INTO "EasyChatz_Connections" ("RequesterUserId", "RecipientUserId", "Status", "Introduction", "CreatedAtUtc", "RespondedAtUtc")
                SELECT
                    CASE WHEN "FromUserId" < "ToUserId" THEN "FromUserId" ELSE "ToUserId" END,
                    CASE WHEN "FromUserId" < "ToUserId" THEN "ToUserId" ELSE "FromUserId" END,
                    1,
                    '',
                    datetime('now'),
                    datetime('now')
                FROM "EasyChatz_Messages"
                WHERE "ToUserId" IS NOT NULL AND "ToUserId" <> "FromUserId"
                GROUP BY
                    CASE WHEN "FromUserId" < "ToUserId" THEN "FromUserId" ELSE "ToUserId" END,
                    CASE WHEN "FromUserId" < "ToUserId" THEN "ToUserId" ELSE "FromUserId" END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EasyChatz_Connections");
        }
    }
}
