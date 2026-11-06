using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyChatz.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class ShortenUserIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AdminUserId",
                schema: "chatz",
                table: "Rooms",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldUnicode: false,
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "ToUserId",
                schema: "chatz",
                table: "Notifications",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldUnicode: false,
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "FromUserId",
                schema: "chatz",
                table: "Notifications",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldUnicode: false,
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "ToUserId",
                schema: "chatz",
                table: "Messages",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldUnicode: false,
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FromUserId",
                schema: "chatz",
                table: "Messages",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldUnicode: false,
                oldMaxLength: 450);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AdminUserId",
                schema: "chatz",
                table: "Rooms",
                type: "character varying(450)",
                unicode: false,
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldUnicode: false,
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ToUserId",
                schema: "chatz",
                table: "Notifications",
                type: "character varying(450)",
                unicode: false,
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldUnicode: false,
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "FromUserId",
                schema: "chatz",
                table: "Notifications",
                type: "character varying(450)",
                unicode: false,
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldUnicode: false,
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ToUserId",
                schema: "chatz",
                table: "Messages",
                type: "character varying(450)",
                unicode: false,
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldUnicode: false,
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FromUserId",
                schema: "chatz",
                table: "Messages",
                type: "character varying(450)",
                unicode: false,
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldUnicode: false,
                oldMaxLength: 128);
        }
    }
}
