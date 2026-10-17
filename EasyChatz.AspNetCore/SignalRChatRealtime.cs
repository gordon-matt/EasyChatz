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
        await hub.Clients.User(message.FromUserId).SendAsync(Constants.HubEvents.NewPrivateMessage, message, cancellationToken);
        if (!string.IsNullOrEmpty(message.ToUserId)
            && !string.Equals(message.ToUserId, message.FromUserId, StringComparison.Ordinal))
        {
            await hub.Clients.User(message.ToUserId).SendAsync(Constants.HubEvents.NewPrivateMessage, message, cancellationToken);
        }
    }

    public async Task PublishMessageRemovedAsync(ChatMessageRemovedDto message, CancellationToken cancellationToken = default)
    {
        if (message.RoomId is not null && !string.IsNullOrEmpty(message.RoomName))
        {
            await hub.Clients.Group(message.RoomName).SendAsync(Constants.HubEvents.RemoveMessage, message, cancellationToken);
            return;
        }

        await hub.Clients.User(message.FromUserId).SendAsync(Constants.HubEvents.RemoveMessage, message, cancellationToken);
        if (!string.IsNullOrEmpty(message.ToUserId))
        {
            await hub.Clients.User(message.ToUserId).SendAsync(Constants.HubEvents.RemoveMessage, message, cancellationToken);
        }
    }

    public Task PublishRoomAddedAsync(ChatRoomDto room, CancellationToken cancellationToken = default) =>
        hub.Clients.All.SendAsync(Constants.HubEvents.AddRoom, room, cancellationToken);

    public Task PublishRoomUpdatedAsync(ChatRoomUpdatedDto room, CancellationToken cancellationToken = default) =>
        hub.Clients.All.SendAsync(Constants.HubEvents.UpdateRoom, room, cancellationToken);

    public async Task PublishRoomRemovedAsync(ChatRoomRemovedDto room, CancellationToken cancellationToken = default)
    {
        await hub.Clients.Group(room.RoomName).SendAsync(Constants.HubEvents.RoomDeleted, room, cancellationToken);
        await hub.Clients.All.SendAsync(Constants.HubEvents.RemoveRoom, room, cancellationToken);
    }
}
