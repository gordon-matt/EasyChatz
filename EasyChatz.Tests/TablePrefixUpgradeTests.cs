using EasyChatz.Data.Entities;
using EasyChatz.Data.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EasyChatz.Tests;

/// <summary>SQLite databases created before the tables were prefixed must keep their data.</summary>
public class TablePrefixUpgradeTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Upgrading_a_database_with_unprefixed_tables_keeps_every_row()
    {
        string connectionString = $"Data Source=ec-upgrade-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync(Token);

        var factory = new EasyChatzDbContextFactory(connectionString);

        // The last migration before the tables were prefixed, seeded with a room, a message in it and a notification.
        await using (var legacy = factory.GetContext())
        {
            await legacy.GetService<IMigrator>().MigrateAsync("20261017070610_NotificationDelivery", Token);
            await legacy.Database.ExecuteSqlRawAsync("""
                INSERT INTO "Rooms" ("Id", "Name", "AdminUserId", "CreatedAtUtc") VALUES (7, 'Announcements', 'system', '2026-01-01');
                INSERT INTO "Messages" ("Id", "Content", "Timestamp", "FromUserId", "ToRoomId", "ToUserId") VALUES
                    (11, 'Hello room', '2026-01-02', 'alex', 7, NULL),
                    (12, 'Hello Blair', '2026-01-03', 'alex', NULL, 'blair');
                INSERT INTO "Notifications" ("Id", "ChatMessageId", "FromUserId", "ToUserId", "IsRead", "CreatedAtUtc") VALUES
                    (21, 12, 'alex', 'blair', 0, '2026-01-03');
                """, Token);
        }

        await using (var upgrading = factory.GetContext())
        {
            await upgrading.Database.MigrateAsync(Token);
        }

        await using var context = factory.GetContext();

        var room = await context.Set<ChatRoom>().SingleAsync(Token);
        Assert.Equal((7, "Announcements"), (room.Id, room.Name));

        var messages = await context.Set<ChatMessage>().OrderBy(m => m.Id).ToListAsync(Token);
        Assert.Equal(["Hello room", "Hello Blair"], messages.Select(m => m.Content));
        Assert.Equal(7, messages[0].ToRoomId);

        var notification = await context.Set<MessageNotification>().SingleAsync(Token);
        Assert.Equal((21, 12, false), (notification.Id, notification.ChatMessageId, notification.IsRead));

        // The old tables are gone, and the foreign keys still point at the renamed ones.
        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT name AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name NOT LIKE '\\_\\_%' ESCAPE '\\' AND name <> 'sqlite_sequence' ORDER BY name")
            .ToListAsync(Token);
        Assert.Equal(["EasyChatz_Messages", "EasyChatz_Notifications", "EasyChatz_Rooms"], tables);

        var violations = await context.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM pragma_foreign_key_check")
            .SingleAsync(Token);
        Assert.Equal(0, violations);

        // Deleting the room still cascades to its messages through the rebuilt foreign key.
        context.Set<ChatRoom>().Remove(room);
        await context.SaveChangesAsync(Token);
        Assert.Equal(["Hello Blair"], await context.Set<ChatMessage>().Select(m => m.Content).ToListAsync(Token));
    }
}
