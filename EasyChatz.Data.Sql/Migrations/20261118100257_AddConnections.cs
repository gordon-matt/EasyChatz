using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyChatz.Data.Sql.Migrations
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterUserId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    RecipientUserId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Introduction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                INSERT INTO [chatz].[Connections] ([RequesterUserId], [RecipientUserId], [Status], [Introduction], [CreatedAtUtc], [RespondedAtUtc])
                SELECT
                    CASE WHEN [FromUserId] < [ToUserId] THEN [FromUserId] ELSE [ToUserId] END,
                    CASE WHEN [FromUserId] < [ToUserId] THEN [ToUserId] ELSE [FromUserId] END,
                    1,
                    N'',
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME()
                FROM [chatz].[Messages]
                WHERE [ToUserId] IS NOT NULL AND [ToUserId] <> [FromUserId]
                GROUP BY
                    CASE WHEN [FromUserId] < [ToUserId] THEN [FromUserId] ELSE [ToUserId] END,
                    CASE WHEN [FromUserId] < [ToUserId] THEN [ToUserId] ELSE [FromUserId] END;
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
