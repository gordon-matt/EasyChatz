using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyChatz.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class NotificationDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAtUtc",
                schema: "chatz",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead_DeliveredAtUtc",
                schema: "chatz",
                table: "Notifications",
                columns: new[] { "IsRead", "DeliveredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_IsRead_DeliveredAtUtc",
                schema: "chatz",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeliveredAtUtc",
                schema: "chatz",
                table: "Notifications");
        }
    }
}
