using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyChatz.Data.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserBlocks_BlockedUserId",
                schema: "chatz",
                table: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_UserBlocks_BlockerUserId_BlockedUserId",
                schema: "chatz",
                table: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_Name",
                schema: "chatz",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ToUserId_FromUserId_IsRead",
                schema: "chatz",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Messages_FromUserId_ToUserId_Id",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ToRoomId_Id",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ToUserId_FromUserId_Id",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Connections_RecipientUserId",
                schema: "chatz",
                table: "Connections");

            migrationBuilder.DropIndex(
                name: "IX_Connections_RequesterUserId_RecipientUserId",
                schema: "chatz",
                table: "Connections");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "chatz",
                table: "UserBlocks",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "chatz",
                table: "Rooms",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "chatz",
                table: "Notifications",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "chatz",
                table: "Messages",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "chatz",
                table: "Connections",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_TenantId_BlockedUserId",
                schema: "chatz",
                table: "UserBlocks",
                columns: new[] { "TenantId", "BlockedUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_TenantId_BlockerUserId_BlockedUserId",
                schema: "chatz",
                table: "UserBlocks",
                columns: new[] { "TenantId", "BlockerUserId", "BlockedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_TenantId_Name",
                schema: "chatz",
                table: "Rooms",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId_ToUserId_FromUserId_IsRead",
                schema: "chatz",
                table: "Notifications",
                columns: new[] { "TenantId", "ToUserId", "FromUserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TenantId_FromUserId_ToUserId_Id",
                schema: "chatz",
                table: "Messages",
                columns: new[] { "TenantId", "FromUserId", "ToUserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TenantId_ToRoomId_Id",
                schema: "chatz",
                table: "Messages",
                columns: new[] { "TenantId", "ToRoomId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TenantId_ToUserId_FromUserId_Id",
                schema: "chatz",
                table: "Messages",
                columns: new[] { "TenantId", "ToUserId", "FromUserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ToRoomId",
                schema: "chatz",
                table: "Messages",
                column: "ToRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_TenantId_RecipientUserId",
                schema: "chatz",
                table: "Connections",
                columns: new[] { "TenantId", "RecipientUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Connections_TenantId_RequesterUserId_RecipientUserId",
                schema: "chatz",
                table: "Connections",
                columns: new[] { "TenantId", "RequesterUserId", "RecipientUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserBlocks_TenantId_BlockedUserId",
                schema: "chatz",
                table: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_UserBlocks_TenantId_BlockerUserId_BlockedUserId",
                schema: "chatz",
                table: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_TenantId_Name",
                schema: "chatz",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TenantId_ToUserId_FromUserId_IsRead",
                schema: "chatz",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Messages_TenantId_FromUserId_ToUserId_Id",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_TenantId_ToRoomId_Id",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_TenantId_ToUserId_FromUserId_Id",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ToRoomId",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Connections_TenantId_RecipientUserId",
                schema: "chatz",
                table: "Connections");

            migrationBuilder.DropIndex(
                name: "IX_Connections_TenantId_RequesterUserId_RecipientUserId",
                schema: "chatz",
                table: "Connections");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "chatz",
                table: "UserBlocks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "chatz",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "chatz",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "chatz",
                table: "Connections");

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

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Name",
                schema: "chatz",
                table: "Rooms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ToUserId_FromUserId_IsRead",
                schema: "chatz",
                table: "Notifications",
                columns: new[] { "ToUserId", "FromUserId", "IsRead" });

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
        }
    }
}
