using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EasyChatz.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "chatz");

            migrationBuilder.CreateTable(
                name: "Rooms",
                schema: "chatz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AdminUserId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "chatz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FromUserId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false),
                    ToRoomId = table.Column<int>(type: "integer", nullable: true),
                    ToUserId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Rooms_ToRoomId",
                        column: x => x.ToRoomId,
                        principalSchema: "chatz",
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "chatz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatMessageId = table.Column<int>(type: "integer", nullable: false),
                    FromUserId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false),
                    ToUserId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Messages_ChatMessageId",
                        column: x => x.ChatMessageId,
                        principalSchema: "chatz",
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_FromUserId_ToUserId_Id",
                schema: "chatz",
                table: "Messages",
                columns: new[] { "FromUserId", "ToUserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ToRoomId_Id",
                schema: "chatz",
                table: "Messages",
                columns: new[] { "ToRoomId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ToUserId_FromUserId_Id",
                schema: "chatz",
                table: "Messages",
                columns: new[] { "ToUserId", "FromUserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ChatMessageId",
                schema: "chatz",
                table: "Notifications",
                column: "ChatMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ToUserId_FromUserId_IsRead",
                schema: "chatz",
                table: "Notifications",
                columns: new[] { "ToUserId", "FromUserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Name",
                schema: "chatz",
                table: "Rooms",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "chatz");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "chatz");

            migrationBuilder.DropTable(
                name: "Rooms",
                schema: "chatz");
        }
    }
}
