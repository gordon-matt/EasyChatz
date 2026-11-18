using System.Security.Claims;
using EasyChatz.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace EasyChatz;

/// <summary>
/// Presence and direct messages. Room messages are saved by <see cref="Services.IChatService"/> and
/// broadcast from there; this hub is what the browser joins so it receives them.
/// </summary>
public sealed class ChatHub(IChatService chat, IChatUserDirectory directory, IChatTenant tenants, IOptions<EasyChatzOptions> options) : Hub
{
    private static readonly Dictionary<string, Connection> Connections = new();
    private static readonly Lock Gate = new();

    /// <summary>
    /// Joins the value.
    /// </summary>
    /// <param name="roomName">The room name.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

        string group = Constants.RoomGroup(connection.TenantId, roomName);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        connection.Room = group;
    }

    /// <summary>
    /// Performs the leave operation.
    /// </summary>
    /// <param name="roomName">The room name.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task Leave(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            return Task.CompletedTask;
        }

        string tenant;
        lock (Gate)
        {
            tenant = Connections.TryGetValue(Context.ConnectionId, out var connection)
                ? connection.TenantId
                : CurrentTenant();
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, Constants.RoomGroup(tenant, roomName));
    }

    /// <summary>Sends the caller the online people they are allowed to see. Used after a connection is accepted.</summary>
    public async Task SyncPresence()
    {
        string? userId = CurrentUserId();
        if (string.IsNullOrEmpty(userId) || IsWatching())
        {
            return;
        }

        string tenant = CurrentTenant();
        List<ChatPresenceDto> existing;
        lock (Gate)
        {
            existing = SameTenant(tenant)
                .Where(person => person.ConnectionId != Context.ConnectionId)
                .ToList();
        }

        await Clients.Caller.SendAsync(Constants.HubEvents.SetOnlineUsers, await VisibleAsync(userId, tenant, existing));
    }

    /// <summary>Puts the connection back in its user group after a reconnect.</summary>
    public async Task Rejoin()
    {
        string? userId = CurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        string tenant = CurrentTenant();
        await Groups.AddToGroupAsync(Context.ConnectionId, Constants.UserGroup(tenant, userId));
        if (!IsWatching() && tenant.Length > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, Constants.TenantGroup(tenant));
        }
    }

    /// <summary>
    /// Sends the private.
    /// </summary>
    /// <param name="toUserId">The to user identifier.</param>
    /// <param name="message">The message.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the chat message.</returns>
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

    /// <inheritdoc/>
    public override async Task OnConnectedAsync()
    {
        string? userId = CurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await base.OnConnectedAsync();
            return;
        }

        string tenant = CurrentTenant();
        await Groups.AddToGroupAsync(Context.ConnectionId, Constants.UserGroup(tenant, userId));
        if (IsWatching())
        {
            await base.OnConnectedAsync();
            return;
        }

        if (tenant.Length > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, Constants.TenantGroup(tenant));
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
            existing = SameTenant(tenant);
            Connections[Context.ConnectionId] = new Connection(presence, tenant);
        }

        await Clients.Caller.SendAsync(Constants.HubEvents.SetOnlineUsers, await VisibleAsync(userId, tenant, existing));
        await PublishPresenceAsync(userId, tenant, Constants.HubEvents.AddUser, presence);
        await base.OnConnectedAsync();
    }

    /// <inheritdoc/>
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
            await PublishPresenceAsync(connection.User.UserId, connection.TenantId, Constants.HubEvents.RemoveUser, connection.User);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private string? CurrentUserId() =>
        Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    private string CurrentTenant() => Constants.TenantKey(tenants.TenantId);

    /// <summary>Caller holds <see cref="Gate"/>.</summary>
    private static List<ChatPresenceDto> SameTenant(string tenant) =>
        Connections.Values
            .Where(connection => connection.TenantId == tenant)
            .Select(connection => connection.User)
            .ToList();

    private async Task<List<ChatPresenceDto>> VisibleAsync(string userId, string tenant, IReadOnlyList<ChatPresenceDto> people)
    {
        var others = people.Where(person => person.ConnectionId != Context.ConnectionId).ToList();
        if (!options.Value.RequireConnections)
        {
            return others;
        }

        var allowed = await chat.GetConnectedUserIdsAsync();
        var ids = allowed.IsSuccess && allowed.Value is not null
            ? allowed.Value.ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);
        return others.Where(person => ids.Contains(person.UserId) && !string.Equals(person.UserId, userId, StringComparison.Ordinal)).ToList();
    }

    private async Task PublishPresenceAsync(string userId, string tenant, string eventName, ChatPresenceDto presence)
    {
        if (!options.Value.RequireConnections)
        {
            if (tenant.Length == 0)
            {
                await Clients.Others.SendAsync(eventName, presence);
                return;
            }

            await Clients.GroupExcept(Constants.TenantGroup(tenant), Context.ConnectionId).SendAsync(eventName, presence);
            return;
        }

        var allowed = await chat.GetConnectedUserIdsAsync();
        if (!allowed.IsSuccess || allowed.Value is null)
        {
            return;
        }

        foreach (string otherId in allowed.Value.Where(id => !string.Equals(id, userId, StringComparison.Ordinal)).Distinct(StringComparer.Ordinal))
        {
            await Clients.Group(Constants.UserGroup(tenant, otherId)).SendAsync(eventName, presence);
        }
    }

    private bool IsWatching()
    {
        var http = Context.GetHttpContext();
        return http?.Request.Query.TryGetValue(Constants.WatchQueryName, out var value) == true && value == "1";
    }

    private sealed class Connection(ChatPresenceDto user, string tenantId)
    {
        public ChatPresenceDto User { get; } = user;

        public string TenantId { get; } = tenantId;

        public string? Room { get; set; }
    }
}
