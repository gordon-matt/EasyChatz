using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyChatz.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UseTablePrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Rooms_ToRoomId",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Messages_ChatMessageId",
                schema: "chatz",
                table: "Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rooms",
                schema: "chatz",
                table: "Rooms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Notifications",
                schema: "chatz",
                table: "Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                schema: "chatz",
                table: "Messages");

            migrationBuilder.RenameTable(
                name: "Rooms",
                schema: "chatz",
                newName: "EasyChatz_Rooms");

            migrationBuilder.RenameTable(
                name: "Notifications",
                schema: "chatz",
                newName: "EasyChatz_Notifications");

            migrationBuilder.RenameTable(
                name: "Messages",
                schema: "chatz",
                newName: "EasyChatz_Messages");

            migrationBuilder.RenameIndex(
                name: "IX_Rooms_Name",
                table: "EasyChatz_Rooms",
                newName: "IX_EasyChatz_Rooms_Name");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_ToUserId_FromUserId_IsRead",
                table: "EasyChatz_Notifications",
                newName: "IX_EasyChatz_Notifications_ToUserId_FromUserId_IsRead");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_IsRead_DeliveredAtUtc",
                table: "EasyChatz_Notifications",
                newName: "IX_EasyChatz_Notifications_IsRead_DeliveredAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_ChatMessageId",
                table: "EasyChatz_Notifications",
                newName: "IX_EasyChatz_Notifications_ChatMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_ToUserId_FromUserId_Id",
                table: "EasyChatz_Messages",
                newName: "IX_EasyChatz_Messages_ToUserId_FromUserId_Id");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_ToRoomId_Id",
                table: "EasyChatz_Messages",
                newName: "IX_EasyChatz_Messages_ToRoomId_Id");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_FromUserId_ToUserId_Id",
                table: "EasyChatz_Messages",
                newName: "IX_EasyChatz_Messages_FromUserId_ToUserId_Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EasyChatz_Rooms",
                table: "EasyChatz_Rooms",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EasyChatz_Notifications",
                table: "EasyChatz_Notifications",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EasyChatz_Messages",
                table: "EasyChatz_Messages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EasyChatz_Messages_EasyChatz_Rooms_ToRoomId",
                table: "EasyChatz_Messages",
                column: "ToRoomId",
                principalTable: "EasyChatz_Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EasyChatz_Notifications_EasyChatz_Messages_ChatMessageId",
                table: "EasyChatz_Notifications",
                column: "ChatMessageId",
                principalTable: "EasyChatz_Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EasyChatz_Messages_EasyChatz_Rooms_ToRoomId",
                table: "EasyChatz_Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_EasyChatz_Notifications_EasyChatz_Messages_ChatMessageId",
                table: "EasyChatz_Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EasyChatz_Rooms",
                table: "EasyChatz_Rooms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EasyChatz_Notifications",
                table: "EasyChatz_Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EasyChatz_Messages",
                table: "EasyChatz_Messages");

            migrationBuilder.EnsureSchema(
                name: "chatz");

            migrationBuilder.RenameTable(
                name: "EasyChatz_Rooms",
                newName: "Rooms",
                newSchema: "chatz");

            migrationBuilder.RenameTable(
                name: "EasyChatz_Notifications",
                newName: "Notifications",
                newSchema: "chatz");

            migrationBuilder.RenameTable(
                name: "EasyChatz_Messages",
                newName: "Messages",
                newSchema: "chatz");

            migrationBuilder.RenameIndex(
                name: "IX_EasyChatz_Rooms_Name",
                schema: "chatz",
                table: "Rooms",
                newName: "IX_Rooms_Name");

            migrationBuilder.RenameIndex(
                name: "IX_EasyChatz_Notifications_ToUserId_FromUserId_IsRead",
                schema: "chatz",
                table: "Notifications",
                newName: "IX_Notifications_ToUserId_FromUserId_IsRead");

            migrationBuilder.RenameIndex(
                name: "IX_EasyChatz_Notifications_IsRead_DeliveredAtUtc",
                schema: "chatz",
                table: "Notifications",
                newName: "IX_Notifications_IsRead_DeliveredAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_EasyChatz_Notifications_ChatMessageId",
                schema: "chatz",
                table: "Notifications",
                newName: "IX_Notifications_ChatMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_EasyChatz_Messages_ToUserId_FromUserId_Id",
                schema: "chatz",
                table: "Messages",
                newName: "IX_Messages_ToUserId_FromUserId_Id");

            migrationBuilder.RenameIndex(
                name: "IX_EasyChatz_Messages_ToRoomId_Id",
                schema: "chatz",
                table: "Messages",
                newName: "IX_Messages_ToRoomId_Id");

            migrationBuilder.RenameIndex(
                name: "IX_EasyChatz_Messages_FromUserId_ToUserId_Id",
                schema: "chatz",
                table: "Messages",
                newName: "IX_Messages_FromUserId_ToUserId_Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rooms",
                schema: "chatz",
                table: "Rooms",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Notifications",
                schema: "chatz",
                table: "Notifications",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                schema: "chatz",
                table: "Messages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Rooms_ToRoomId",
                schema: "chatz",
                table: "Messages",
                column: "ToRoomId",
                principalSchema: "chatz",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Messages_ChatMessageId",
                schema: "chatz",
                table: "Notifications",
                column: "ChatMessageId",
                principalSchema: "chatz",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
