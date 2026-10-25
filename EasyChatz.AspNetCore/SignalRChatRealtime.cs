using EasyChatz.Services;
using Microsoft.AspNetCore.SignalR;

namespace EasyChatz;

/// <summary>Fans chat changes out through <see cref="ChatHub"/>.</summary>
internal sealed class SignalRChatRealtime(IHubContext<ChatHub> hub) : IChatRealtime
{
    public Task PublishRoomMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default) =>
        string.IsNullOrEmpty(message.ToRoomName)
            ? Task.CompletedTask
            : hub.Clients.Group(message.ToRoomName).SendAsync(Constants.HubEvents.NewMessage, message, cancellationToken);

    public async Task PublishPrivateMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default)
    {
        await UserAsync(message.FromUserId, Constants.HubEvents.NewPrivateMessage, message, cancellationToken);
        if (!string.IsNullOrEmpty(message.ToUserId)
            && !string.Equals(message.ToUserId, message.FromUserId, StringComparison.Ordinal))
        {
            await UserAsync(message.ToUserId, Constants.HubEvents.NewPrivateMessage, message, cancellationToken);
        }
    }

    public async Task PublishMessageRemovedAsync(ChatMessageRemovedDto message, CancellationToken cancellationToken = default)
    {
        if (message.RoomId is not null && !string.IsNullOrEmpty(message.RoomName))
        {
            await hub.Clients.Group(message.RoomName).SendAsync(Constants.HubEvents.RemoveMessage, message, cancellationToken);
            return;
        }

        await UserAsync(message.FromUserId, Constants.HubEvents.RemoveMessage, message, cancellationToken);
        if (!string.IsNullOrEmpty(message.ToUserId))
        {
            await UserAsync(message.ToUserId, Constants.HubEvents.RemoveMessage, message, cancellationToken);
        }
    }

    private async Task UserAsync(string userId, string eventName, object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Group covers the connection we tracked. User() covers a reconnect that dropped the group.
        // The browser ignores the second copy of the same message.
        await hub.Clients.Group(Constants.UserGroup(userId)).SendAsync(eventName, payload, cancellationToken);
        await hub.Clients.User(userId).SendAsync(eventName, payload, cancellationToken);
    }

    public Task PublishRoomAddedAsync(ChatRoomDto room, CancellationToken cancellationToken = default) =>
        hub.Clients.All.SendAsync(Constants.HubEvents.AddRoom, room, cancellationToken);

    public Task PublishRoomUpdatedAsync(ChatRoomUpdatedDto room, CancellationToken cancellationToken = default) =>
        hub.Clients.All.SendAsync(Constants.HubEvents.UpdateRoom, room, cancellationToken);

    public Task PublishUnreadChangedAsync(string userId, CancellationToken cancellationToken = default) =>
        UserAsync(userId, Constants.HubEvents.UnreadChanged, new { userId }, cancellationToken);

    public async Task PublishMentionAsync(ChatMessageDto message, IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default)
    {
        foreach (string userId in userIds.Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal))
        {
            await UserAsync(userId, Constants.HubEvents.NewMessage, message, cancellationToken);
        }
    }

    public async Task PublishRoomRemovedAsync(ChatRoomRemovedDto room, CancellationToken cancellationToken = default)
    {
        await hub.Clients.Group(room.RoomName).SendAsync(Constants.HubEvents.RoomDeleted, room, cancellationToken);
        await hub.Clients.All.SendAsync(Constants.HubEvents.RemoveRoom, room, cancellationToken);
    }
}
