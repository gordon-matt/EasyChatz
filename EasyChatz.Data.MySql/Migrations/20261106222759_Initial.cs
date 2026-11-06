using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace EasyChatz.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EasyChatz_Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    AdminUserId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EasyChatz_Rooms", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EasyChatz_Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Content = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FromUserId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    ToRoomId = table.Column<int>(type: "int", nullable: true),
                    ToUserId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EasyChatz_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EasyChatz_Messages_EasyChatz_Rooms_ToRoomId",
                        column: x => x.ToRoomId,
                        principalTable: "EasyChatz_Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EasyChatz_Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ChatMessageId = table.Column<int>(type: "int", nullable: false),
                    FromUserId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    ToUserId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EasyChatz_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EasyChatz_Notifications_EasyChatz_Messages_ChatMessageId",
                        column: x => x.ChatMessageId,
                        principalTable: "EasyChatz_Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Messages_FromUserId_ToUserId_Id",
                table: "EasyChatz_Messages",
                columns: new[] { "FromUserId", "ToUserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Messages_ToRoomId_Id",
                table: "EasyChatz_Messages",
                columns: new[] { "ToRoomId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Messages_ToUserId_FromUserId_Id",
                table: "EasyChatz_Messages",
                columns: new[] { "ToUserId", "FromUserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Notifications_ChatMessageId",
                table: "EasyChatz_Notifications",
                column: "ChatMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Notifications_IsRead_DeliveredAtUtc",
                table: "EasyChatz_Notifications",
                columns: new[] { "IsRead", "DeliveredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Notifications_ToUserId_FromUserId_IsRead",
                table: "EasyChatz_Notifications",
                columns: new[] { "ToUserId", "FromUserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Rooms_Name",
                table: "EasyChatz_Rooms",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EasyChatz_Notifications");

            migrationBuilder.DropTable(
                name: "EasyChatz_Messages");

            migrationBuilder.DropTable(
                name: "EasyChatz_Rooms");
        }
    }
}
