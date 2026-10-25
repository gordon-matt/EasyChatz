using Ardalis.Result;
using EasyChatz.Models;
using EasyChatz.Services;

namespace EasyChatz.Tests;

public sealed class ChatServiceTests
{
    [Fact]
    public async Task Announcements_comes_first_and_members_cannot_post()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await TestDatabase.CreateAsync();
        database.CurrentUser.User = TestDirectory.Blair;

        var created = await database.Chat.CreateRoomAsync("General", cancellationToken);
        Assert.True(created.IsSuccess);

        var rooms = await database.Chat.GetRoomsAsync(cancellationToken);
        Assert.Equal("Announcements", rooms.Value![0].Name);
        Assert.Equal("General", rooms.Value[1].Name);

        var announcements = rooms.Value[0];
        var denied = await database.Chat.SendRoomMessageAsync(announcements.Id, "hello", cancellationToken);
        Assert.Equal(ResultStatus.Forbidden, denied.Status);

        var renamed = await database.Chat.UpdateRoomAsync(announcements.Id, "News", cancellationToken);
        Assert.Equal(ResultStatus.Forbidden, renamed.Status);

        var deleted = await database.Chat.DeleteRoomAsync(announcements.Id, cancellationToken);
        Assert.Equal(ResultStatus.Forbidden, deleted.Status);
    }

    [Fact]
    public async Task Administrator_can_post_in_announcements_and_names_must_be_unique()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await TestDatabase.CreateAsync();

        var rooms = await database.Chat.GetRoomsAsync(cancellationToken);
        var posted = await database.Chat.SendRoomMessageAsync(rooms.Value![0].Id, "<b>Welcome</b>", cancellationToken);
        Assert.True(posted.IsSuccess);
        Assert.Equal("Welcome", posted.Value!.Content);
        Assert.Equal("Alex Morgan", posted.Value.FromDisplayName);

        var taken = await database.Chat.CreateRoomAsync("announcements", cancellationToken);
        Assert.Equal(ResultStatus.Forbidden, taken.Status);

        await database.Chat.CreateRoomAsync("General", cancellationToken);
        var conflict = await database.Chat.CreateRoomAsync(" general ", cancellationToken);
        Assert.Equal(ResultStatus.Conflict, conflict.Status);
    }

    [Fact]
    public async Task Direct_messages_track_unread_until_the_conversation_is_opened()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await TestDatabase.CreateAsync();

        var sent = await database.Chat.SendPrivateMessageAsync("blair", "Are you there?", cancellationToken);
        Assert.True(sent.IsSuccess);

        database.CurrentUser.User = TestDirectory.Blair;
        var unread = await database.Notifications.GetUnreadAsync(cancellationToken);
        Assert.Equal(1, unread.Value!.Single().UnreadCount);
        Assert.Equal("Alex Morgan", unread.Value.Single().DisplayName);

        var conversations = await database.Chat.GetConversationsAsync(cancellationToken);
        Assert.Equal("alex", conversations.Value!.Single().UserId);
        Assert.Equal(1, conversations.Value.Single().UnreadCount);

        var marked = await database.Notifications.MarkReadAsync("alex", cancellationToken);
        Assert.True(marked.IsSuccess);

        var after = await database.Chat.GetConversationsAsync(cancellationToken);
        Assert.Equal(0, after.Value!.Single().UnreadCount);
    }

    [Fact]
    public async Task Pending_delivery_is_unread_mail_a_host_has_not_taken()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await TestDatabase.CreateAsync();
        var queue = database.Delivery;

        var sent = await database.Chat.SendPrivateMessageAsync("blair", "Bring the album", cancellationToken);
        Assert.True(sent.IsSuccess);

        var waiting = await queue.ListPendingAsync(DateTime.UtcNow.AddMinutes(1), cancellationToken);
        var notice = Assert.Single(waiting);
        Assert.Equal("alex", notice.FromUserId);
        Assert.Equal("blair", notice.ToUserId);
        Assert.Equal("Bring the album", notice.Content);

        await queue.MarkDeliveredAsync([notice.Id], cancellationToken);
        var after = await queue.ListPendingAsync(DateTime.UtcNow.AddMinutes(1), cancellationToken);
        Assert.Empty(after);

        database.CurrentUser.User = TestDirectory.Blair;
        var stillUnread = await database.Notifications.GetUnreadAsync(cancellationToken);
        Assert.Equal(1, stillUnread.Value!.Single().UnreadCount);
    }

    [Fact]
    public async Task Only_the_sender_can_delete_a_message_and_pages_walk_backwards()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await TestDatabase.CreateAsync();
        var room = (await database.Chat.CreateRoomAsync("General", cancellationToken)).Value!;

        for (int i = 1; i <= 3; i++)
        {
            await database.Chat.SendRoomMessageAsync(room.Id, $"Message {i}", cancellationToken);
        }

        var latest = await database.Chat.GetRoomMessagesAsync(room.Id, take: 2, cancellationToken: cancellationToken);
        Assert.Equal(["Message 2", "Message 3"], latest.Value!.Select(m => m.Content).ToArray());

        var older = await database.Chat.GetRoomMessagesAsync(room.Id, take: 2, beforeId: latest.Value![0].Id, cancellationToken);
        Assert.Equal(["Message 1"], older.Value!.Select(m => m.Content).ToArray());

        database.CurrentUser.User = TestDirectory.Blair;
        var denied = await database.Chat.DeleteMessageAsync(latest.Value[0].Id, cancellationToken);
        Assert.Equal(ResultStatus.Forbidden, denied.Status);

        database.CurrentUser.User = TestDirectory.Alex;
        var removed = await database.Chat.DeleteMessageAsync(latest.Value[^1].Id, cancellationToken);
        Assert.True(removed.IsSuccess);

        var remaining = await database.Chat.GetRoomMessagesAsync(room.Id, take: 10, cancellationToken: cancellationToken);
        Assert.DoesNotContain(remaining.Value!, m => m.Content == "Message 3");
    }
    [Fact]
    public async Task Room_mentions_are_rewritten_with_the_real_name_and_unknown_people_become_plain_text()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await TestDatabase.CreateAsync();
        var room = (await database.Chat.CreateRoomAsync("General", cancellationToken)).Value!;

        var posted = await database.Chat.SendRoomMessageAsync(
            room.Id, "Hi @[Fake Name](blair) and @[Ghost](nobody)", cancellationToken);

        Assert.True(posted.IsSuccess);
        Assert.Equal("Hi @[Blair Chen](blair) and @Ghost", posted.Value!.Content);
    }

    [Fact]
    public async Task Direct_messages_flatten_mentions()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await TestDatabase.CreateAsync();

        var sent = await database.Chat.SendPrivateMessageAsync("blair", "Hey @[Blair Chen](blair)", cancellationToken);

        Assert.Equal("Hey @Blair Chen", sent.Value!.Content);
    }

    [Fact]
    public async Task Mention_search_leaves_out_the_signed_in_user()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await TestDatabase.CreateAsync();

        var found = await database.Chat.SearchMentionsAsync("a", 8, cancellationToken);

        Assert.True(found.IsSuccess);
        Assert.Equal("blair", found.Value!.Single().UserId);
    }
}
