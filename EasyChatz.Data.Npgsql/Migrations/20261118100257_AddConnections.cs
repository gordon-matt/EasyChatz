using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EasyChatz.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Connections",
                schema: "chatz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequesterUserId = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    RecipientUserId = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Introduction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Connections_RecipientUserId",
                schema: "chatz",
                table: "Connections",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_RequesterUserId_RecipientUserId",
                schema: "chatz",
                table: "Connections",
                columns: new[] { "RequesterUserId", "RecipientUserId" },
                unique: true);

            // People who already exchanged a direct message are connected, so turning
            // RequireConnections on later does not separate them.
            migrationBuilder.Sql(
                """
                INSERT INTO chatz."Connections" ("RequesterUserId", "RecipientUserId", "Status", "Introduction", "CreatedAtUtc", "RespondedAtUtc")
                SELECT
                    CASE WHEN "FromUserId" < "ToUserId" THEN "FromUserId" ELSE "ToUserId" END,
                    CASE WHEN "FromUserId" < "ToUserId" THEN "ToUserId" ELSE "FromUserId" END,
                    1,
                    '',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM chatz."Messages"
                WHERE "ToUserId" IS NOT NULL AND "ToUserId" <> "FromUserId"
                GROUP BY 1, 2;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Connections",
                schema: "chatz");
        }
    }
}
