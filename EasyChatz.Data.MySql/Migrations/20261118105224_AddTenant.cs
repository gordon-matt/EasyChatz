using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyChatz.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_UserBlocks_BlockedUserId",
                table: "EasyChatz_UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_UserBlocks_BlockerUserId_BlockedUserId",
                table: "EasyChatz_UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Rooms_Name",
                table: "EasyChatz_Rooms");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Notifications_ToUserId_FromUserId_IsRead",
                table: "EasyChatz_Notifications");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Messages_FromUserId_ToUserId_Id",
                table: "EasyChatz_Messages");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Messages_ToRoomId_Id",
                table: "EasyChatz_Messages");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Messages_ToUserId_FromUserId_Id",
                table: "EasyChatz_Messages");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Connections_RecipientUserId",
                table: "EasyChatz_Connections");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Connections_RequesterUserId_RecipientUserId",
                table: "EasyChatz_Connections");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "EasyChatz_UserBlocks",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "EasyChatz_Rooms",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "EasyChatz_Notifications",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "EasyChatz_Messages",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "EasyChatz_Connections",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_UserBlocks_TenantId_BlockedUserId",
                table: "EasyChatz_UserBlocks",
                columns: new[] { "TenantId", "BlockedUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_UserBlocks_TenantId_BlockerUserId_BlockedUserId",
                table: "EasyChatz_UserBlocks",
                columns: new[] { "TenantId", "BlockerUserId", "BlockedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Rooms_TenantId_Name",
                table: "EasyChatz_Rooms",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Notifications_TenantId_ToUserId_FromUserId_IsRead",
                table: "EasyChatz_Notifications",
                columns: new[] { "TenantId", "ToUserId", "FromUserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Messages_TenantId_FromUserId_ToUserId_Id",
                table: "EasyChatz_Messages",
                columns: new[] { "TenantId", "FromUserId", "ToUserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Messages_TenantId_ToRoomId_Id",
                table: "EasyChatz_Messages",
                columns: new[] { "TenantId", "ToRoomId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Messages_TenantId_ToUserId_FromUserId_Id",
                table: "EasyChatz_Messages",
                columns: new[] { "TenantId", "ToUserId", "FromUserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Messages_ToRoomId",
                table: "EasyChatz_Messages",
                column: "ToRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Connections_TenantId_RecipientUserId",
                table: "EasyChatz_Connections",
                columns: new[] { "TenantId", "RecipientUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Connections_TenantId_RequesterUserId_RecipientUser~",
                table: "EasyChatz_Connections",
                columns: new[] { "TenantId", "RequesterUserId", "RecipientUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_UserBlocks_TenantId_BlockedUserId",
                table: "EasyChatz_UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_UserBlocks_TenantId_BlockerUserId_BlockedUserId",
                table: "EasyChatz_UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Rooms_TenantId_Name",
                table: "EasyChatz_Rooms");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Notifications_TenantId_ToUserId_FromUserId_IsRead",
                table: "EasyChatz_Notifications");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Messages_TenantId_FromUserId_ToUserId_Id",
                table: "EasyChatz_Messages");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Messages_TenantId_ToRoomId_Id",
                table: "EasyChatz_Messages");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Messages_TenantId_ToUserId_FromUserId_Id",
                table: "EasyChatz_Messages");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Messages_ToRoomId",
                table: "EasyChatz_Messages");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Connections_TenantId_RecipientUserId",
                table: "EasyChatz_Connections");

            migrationBuilder.DropIndex(
                name: "IX_EasyChatz_Connections_TenantId_RequesterUserId_RecipientUser~",
                table: "EasyChatz_Connections");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EasyChatz_UserBlocks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EasyChatz_Rooms");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EasyChatz_Notifications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EasyChatz_Messages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EasyChatz_Connections");

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_UserBlocks_BlockedUserId",
                table: "EasyChatz_UserBlocks",
                column: "BlockedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_UserBlocks_BlockerUserId_BlockedUserId",
                table: "EasyChatz_UserBlocks",
                columns: new[] { "BlockerUserId", "BlockedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Rooms_Name",
                table: "EasyChatz_Rooms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Notifications_ToUserId_FromUserId_IsRead",
                table: "EasyChatz_Notifications",
                columns: new[] { "ToUserId", "FromUserId", "IsRead" });

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
                name: "IX_EasyChatz_Connections_RecipientUserId",
                table: "EasyChatz_Connections",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EasyChatz_Connections_RequesterUserId_RecipientUserId",
                table: "EasyChatz_Connections",
                columns: new[] { "RequesterUserId", "RecipientUserId" },
                unique: true);
        }
    }
}
