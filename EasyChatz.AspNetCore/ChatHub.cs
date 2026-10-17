using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace EasyChatz;

/// <summary>
/// Presence and direct messages. Room messages are saved by <see cref="Services.IChatService"/> and
/// broadcast from there; this hub is what the browser joins so it receives them.
/// </summary>
public sealed class ChatHub(IChatService chat, IChatUserDirectory directory) : Hub
{
    private static readonly Dictionary<string, Connection> Connections = new();
    private static readonly Lock Gate = new();

    public async Task Join(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            return;
        }

        Connection? connection;
        lock (Gate)
        {
            Connections.TryGetValue(Context.ConnectionId, out connection);
        }

        if (connection is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(connection.Room))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, connection.Room);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        connection.Room = roomName;
    }

    public Task Leave(string roomName) => Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

    /// <summary>Puts the connection back in its user group after a reconnect.</summary>
    public Task Rejoin()
    {
        string? userId = Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(userId)
            ? Task.CompletedTask
            : Groups.AddToGroupAsync(Context.ConnectionId, Constants.UserGroup(userId));
    }

    public async Task<ChatMessageDto?> SendPrivate(string toUserId, string message)
    {
        var result = await chat.SendPrivateMessageAsync(toUserId, message);
        if (!result.IsSuccess || result.Value is null)
        {
            await Clients.Caller.SendAsync(Constants.HubEvents.Error, result.GetDisplayMessage());
            return null;
        }

        return result.Value;
    }

    public override async Task OnConnectedAsync()
    {
        string? userId = Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await base.OnConnectedAsync();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, Constants.UserGroup(userId));
        if (IsWatching())
        {
            await base.OnConnectedAsync();
            return;
        }

        var person = await directory.FindAsync(userId);
        bool administrator = person?.IsAdministrator == true
            || Context.User?.IsInRole("Administrator") == true
            || Context.User?.HasClaim("ec-admin", "true") == true;
        string displayName = person?.DisplayName ?? Context.User?.Identity?.Name ?? userId;
        var presence = new ChatPresenceDto(Context.ConnectionId, userId, displayName, administrator);

        List<ChatPresenceDto> existing;
        lock (Gate)
        {
            existing = Connections.Values.Select(c => c.User).ToList();
            Connections[Context.ConnectionId] = new Connection(presence);
        }

        await Clients.Caller.SendAsync(Constants.HubEvents.SetOnlineUsers, existing);
        await Clients.Others.SendAsync(Constants.HubEvents.AddUser, presence);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Connection? connection;
        lock (Gate)
        {
            Connections.TryGetValue(Context.ConnectionId, out connection);
            Connections.Remove(Context.ConnectionId);
        }

        if (connection is not null)
        {
            await Clients.Others.SendAsync(Constants.HubEvents.RemoveUser, connection.User);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private bool IsWatching()
    {
        var http = Context.GetHttpContext();
        return http?.Request.Query.TryGetValue(Constants.WatchQueryName, out var value) == true && value == "1";
    }

    private sealed class Connection(ChatPresenceDto user)
    {
        public ChatPresenceDto User { get; } = user;

        public string? Room { get; set; }
    }
}
