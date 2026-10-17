using System.Text.Json.Serialization.Metadata;
using EasyChatz.Components.Http;
using Microsoft.AspNetCore.Components.Web;

namespace EasyChatz.Components;

public partial class Chat : ComponentBase, IAsyncDisposable
{
    private const int PageSize = 50;

    private readonly string sessionId = Guid.NewGuid().ToString("N");
    private readonly List<ChatRoomDto> rooms = [];
    private readonly List<ChatConversationDto> conversations = [];
    private readonly Dictionary<int, int> roomUnread = [];
    private readonly List<ChatMessageDto> messages = [];
    private readonly List<ChatPresenceDto> online = [];

    private ChatUser? me;
    private ChatRoomDto? activeRoom;
    private ChatConversationDto? activeConversation;
    private IJSObjectReference? module;
    private DotNetObjectReference<Chat>? self;
    private ElementReference root;
    private ElementReference scroller;
    private ElementReference composer;
    private bool composerBound;
    private bool connected;
    private bool peopleOpen;
    private bool newRoomOpen;
    private bool renameOpen;
    private bool confirmDelete;
    private bool loadingOlder;
    private bool busy;
    private bool scrollPending;
    private bool hasMore;
    private int? restoreFromHeight;
    private string filter = string.Empty;
    private string draft = string.Empty;
    private string newRoomName = string.Empty;
    private string renameName = string.Empty;
    private string? error;

    [Inject]
    private IChatService ChatService { get; set; } = default!;

    [Inject]
    private IChatNotificationService Notifications { get; set; } = default!;

    [Inject]
    private EasyChatzApiAddress Address { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    [Parameter]
    public string Theme { get; set; } = Constants.Themes.Auto;

    [Parameter]
    public string Height { get; set; } = "640px";

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public bool ShowPeople { get; set; } = true;

    [Parameter]
    public EventCallback<ChatConversationEvent> OnConversationChanged { get; set; }

    [Parameter]
    public EventCallback<ChatMessageDto> OnMessage { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private int MaxMessageLength => Constants.MaxLengths.Message;

    private bool CanSend => !busy && !string.IsNullOrWhiteSpace(draft) && !ReadOnlyThread;

    private bool ReadOnlyThread => activeRoom is not null && IsAnnouncements(activeRoom) && me?.IsAdministrator != true;

    private bool CanRename => activeRoom is not null && me is not null && !IsAnnouncements(activeRoom)
        && string.Equals(activeRoom.AdminUserId, me.Id, StringComparison.Ordinal);

    private bool CanDeleteRoom => activeRoom is not null && me is not null && !IsAnnouncements(activeRoom)
        && (me.IsAdministrator || string.Equals(activeRoom.AdminUserId, me.Id, StringComparison.Ordinal));

    private IEnumerable<ChatRoomDto> VisibleRooms => string.IsNullOrWhiteSpace(filter)
        ? rooms
        : rooms.Where(r => r.Name.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase));

    private IEnumerable<ChatConversationDto> VisibleConversations => string.IsNullOrWhiteSpace(filter)
        ? conversations
        : conversations.Where(c => c.DisplayName.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase));

    private IEnumerable<ChatPresenceDto> OtherPeople => online
        .Where(p => me is null || !string.Equals(p.UserId, me.Id, StringComparison.Ordinal))
        .GroupBy(p => p.UserId, StringComparer.Ordinal)
        .Select(g => g.First());

    protected override async Task OnInitializedAsync()
    {
        var current = await ChatService.GetCurrentUserAsync();
        if (!current.IsSuccess)
        {
            error = current.GetDisplayMessage();
            return;
        }

        me = current.Value;
        await ReloadListsAsync();
        if (rooms.Count > 0)
        {
            await OpenRoomAsync(rooms[0]);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && me is not null)
        {
            await ConnectAsync();
        }

        if (module is null)
        {
            return;
        }

        if (!composerBound && !ReadOnlyThread && (activeRoom is not null || activeConversation is not null))
        {
            try
            {
                await module.InvokeVoidAsync("bindComposer", sessionId, composer);
                composerBound = true;
            }
            catch (JSException)
            {
                composerBound = false;
            }
        }

        if (scrollPending)
        {
            scrollPending = false;
            await module.InvokeVoidAsync("scrollToEnd", scroller);
        }

        if (restoreFromHeight is int previous)
        {
            restoreFromHeight = null;
            await module.InvokeVoidAsync("restoreHeight", scroller, previous);
        }
    }

    private async Task ConnectAsync()
    {
        module = await Js.InvokeAsync<IJSObjectReference>("import", "./_content/EasyChatz.Components/easychatz.js");
        self = DotNetObjectReference.Create(this);
        await module.InvokeVoidAsync("connect", sessionId, Address.HubUrl, Address.IncludeCredentials, self);
        connected = true;
        if (activeRoom is not null)
        {
            await module.InvokeVoidAsync("join", sessionId, activeRoom.Name);
        }
    }

    private async Task ReloadListsAsync()
    {
        var roomResult = await ChatService.GetRoomsAsync();
        if (roomResult.IsSuccess && roomResult.Value is not null)
        {
            rooms.Clear();
            rooms.AddRange(roomResult.Value);
        }

        var conversationResult = await ChatService.GetConversationsAsync();
        if (conversationResult.IsSuccess && conversationResult.Value is not null)
        {
            conversations.Clear();
            conversations.AddRange(conversationResult.Value);
        }
    }

    private async Task OpenRoomAsync(ChatRoomDto room)
    {
        activeRoom = room;
        activeConversation = null;
        composerBound = false;
        roomUnread.Remove(room.Id);
        error = null;
        messages.Clear();
        await LoadMessagesAsync(beforeId: null, older: false);
        if (connected && module is not null)
        {
            await module.InvokeVoidAsync("join", sessionId, room.Name);
        }

        await PublishConversationAsync(new ChatConversationEvent("room", room.Id.ToString(), room.Name));
    }

    private async Task OpenConversationAsync(ChatConversationDto conversation)
    {
        activeConversation = conversation;
        activeRoom = null;
        composerBound = false;
        error = null;
        messages.Clear();
        await Notifications.MarkReadAsync(conversation.UserId);
        ReplaceConversation(conversation with { UnreadCount = 0 });
        await LoadMessagesAsync(beforeId: null, older: false);
        await PublishConversationAsync(new ChatConversationEvent("private", conversation.UserId, conversation.DisplayName));
    }

    private async Task MessagePersonAsync(ChatPresenceDto person)
    {
        var existing = conversations.FirstOrDefault(c => string.Equals(c.UserId, person.UserId, StringComparison.Ordinal));
        var conversation = existing ?? new ChatConversationDto(person.UserId, person.DisplayName, 0);
        if (existing is null)
        {
            conversations.Insert(0, conversation);
        }

        peopleOpen = false;
        await OpenConversationAsync(conversation);
    }

    private async Task LoadMessagesAsync(int? beforeId, bool older)
    {
        if (older)
        {
            loadingOlder = true;
            if (module is not null)
            {
                restoreFromHeight = await module.InvokeAsync<int>("captureHeight", scroller);
            }
        }

        Result<IReadOnlyList<ChatMessageDto>> result = activeRoom is not null
            ? await ChatService.GetRoomMessagesAsync(activeRoom.Id, PageSize, beforeId)
            : activeConversation is not null
                ? await ChatService.GetPrivateMessagesAsync(activeConversation.UserId, PageSize, beforeId)
                : Result<IReadOnlyList<ChatMessageDto>>.Success([]);

        loadingOlder = false;
        if (!result.IsSuccess || result.Value is null)
        {
            error = result.GetDisplayMessage();
            return;
        }

        hasMore = result.Value.Count >= PageSize;
        if (older)
        {
            var fresh = result.Value.Where(m => messages.All(existing => existing.Id != m.Id)).ToList();
            messages.InsertRange(0, fresh);
        }
        else
        {
            messages.Clear();
            messages.AddRange(result.Value);
            scrollPending = true;
        }
    }

    private async Task OnScrollAsync()
    {
        if (module is null || loadingOlder || !hasMore || messages.Count == 0)
        {
            return;
        }

        if (!await module.InvokeAsync<bool>("isNearTop", scroller))
        {
            return;
        }

        await LoadMessagesAsync(messages[0].Id, older: true);
    }

    private async Task SendAsync()
    {
        if (!CanSend || me is null)
        {
            return;
        }

        string text = draft;
        busy = true;
        error = null;

        if (activeRoom is not null)
        {
            var sent = await ChatService.SendRoomMessageAsync(activeRoom.Id, text);
            busy = false;
            if (!sent.IsSuccess || sent.Value is null)
            {
                error = sent.GetDisplayMessage();
                return;
            }

            draft = string.Empty;
            AddMessage(sent.Value);
            scrollPending = true;
            await OnMessage.InvokeAsync(sent.Value);
            return;
        }

        if (activeConversation is null || module is null)
        {
            busy = false;
            error = connected ? "Choose a conversation first." : "Still connecting…";
            return;
        }

        try
        {
            await module.InvokeVoidAsync("sendPrivate", sessionId, activeConversation.UserId, text);
            draft = string.Empty;
        }
        catch (JSException)
        {
            error = "The message could not be sent.";
        }
        finally
        {
            busy = false;
        }
    }

    [JSInvokable]
    public Task SendFromComposer() => SendAsync();

    private Task OnDraft(ChangeEventArgs args)
    {
        draft = args.Value?.ToString() ?? string.Empty;
        return Task.CompletedTask;
    }

    private async Task DeleteMessageAsync(int messageId)
    {
        var deleted = await ChatService.DeleteMessageAsync(messageId);
        if (!deleted.IsSuccess)
        {
            error = deleted.GetDisplayMessage();
            return;
        }

        messages.RemoveAll(m => m.Id == messageId);
    }

    private void OpenNewRoom()
    {
        newRoomName = string.Empty;
        newRoomOpen = true;
    }

    private async Task CreateRoomAsync()
    {
        var created = await ChatService.CreateRoomAsync(newRoomName);
        newRoomOpen = false;
        if (!created.IsSuccess || created.Value is null)
        {
            error = created.GetDisplayMessage();
            return;
        }

        UpsertRoom(created.Value);
        await OpenRoomAsync(created.Value);
    }

    private void OpenRename()
    {
        renameName = activeRoom?.Name ?? string.Empty;
        renameOpen = true;
    }

    private async Task RenameRoomAsync()
    {
        if (activeRoom is null)
        {
            return;
        }

        var renamed = await ChatService.UpdateRoomAsync(activeRoom.Id, renameName);
        renameOpen = false;
        if (!renamed.IsSuccess || renamed.Value is null)
        {
            error = renamed.GetDisplayMessage();
            return;
        }

        ApplyRename(renamed.Value);
    }

    private async Task DeleteRoomAsync()
    {
        if (activeRoom is null)
        {
            return;
        }

        int roomId = activeRoom.Id;
        var deleted = await ChatService.DeleteRoomAsync(roomId);
        confirmDelete = false;
        if (!deleted.IsSuccess || deleted.Value is null)
        {
            error = deleted.GetDisplayMessage();
            return;
        }

        RemoveRoom(deleted.Value.RoomId);
    }

    [JSInvokable]
    public async Task OnRoomMessage(string json)
    {
        var message = Read(json, EasyChatzJsonContext.Default.ChatMessageDto);
        if (message is null)
        {
            return;
        }

        if (activeRoom is not null && message.ToRoomId == activeRoom.Id)
        {
            AddMessage(message);
            scrollPending = true;
        }
        else if (message.ToRoomId is int roomId && !string.Equals(message.FromUserId, me?.Id, StringComparison.Ordinal))
        {
            roomUnread[roomId] = roomUnread.GetValueOrDefault(roomId) + 1;
        }

        await OnMessage.InvokeAsync(message);
    }

    [JSInvokable]
    public async Task OnPrivateMessage(string json)
    {
        var message = Read(json, EasyChatzJsonContext.Default.ChatMessageDto);
        if (message is null || me is null)
        {
            return;
        }

        string partnerId = string.Equals(message.FromUserId, me.Id, StringComparison.Ordinal)
            ? message.ToUserId ?? string.Empty
            : message.FromUserId;
        bool open = activeConversation is not null && string.Equals(activeConversation.UserId, partnerId, StringComparison.Ordinal);
        if (open)
        {
            AddMessage(message);
            scrollPending = true;
            if (!string.Equals(message.FromUserId, me.Id, StringComparison.Ordinal))
            {
                await Notifications.MarkReadAsync(partnerId);
            }
        }
        else if (!string.Equals(message.FromUserId, me.Id, StringComparison.Ordinal))
        {
            BumpConversation(partnerId, message.FromDisplayName);
        }
        else
        {
            EnsureConversation(partnerId, message.ToUserId == me.Id ? message.FromDisplayName : partnerId);
        }

        await OnMessage.InvokeAsync(message);
    }

    [JSInvokable]
    public Task OnMessageRemoved(string json)
    {
        var removed = Read(json, EasyChatzJsonContext.Default.ChatMessageRemovedDto);
        if (removed is not null)
        {
            messages.RemoveAll(m => m.Id == removed.MessageId);
        }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnRoomAdded(string json)
    {
        var room = Read(json, EasyChatzJsonContext.Default.ChatRoomDto);
        if (room is not null)
        {
            UpsertRoom(room);
        }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnRoomUpdated(string json)
    {
        var room = Read(json, EasyChatzJsonContext.Default.ChatRoomUpdatedDto);
        if (room is null)
        {
            return;
        }

        ApplyRename(room);
        if (activeRoom?.Id == room.Id && module is not null && !string.Equals(room.PreviousName, room.Name, StringComparison.Ordinal))
        {
            await module.InvokeVoidAsync("join", sessionId, room.Name);
        }
    }

    [JSInvokable]
    public Task OnRoomRemoved(string json)
    {
        var room = Read(json, EasyChatzJsonContext.Default.ChatRoomRemovedDto);
        if (room is not null)
        {
            RemoveRoom(room.RoomId);
        }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnRoomDeleted(string json) => OnRoomRemoved(json);

    [JSInvokable]
    public Task OnOnlineUsers(string json)
    {
        online.Clear();
        var people = Read(json, EasyChatzJsonContext.Default.ListChatPresenceDto);
        if (people is not null)
        {
            online.AddRange(people);
        }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnUserJoined(string json)
    {
        var person = Read(json, EasyChatzJsonContext.Default.ChatPresenceDto);
        if (person is not null && online.All(p => p.ConnectionId != person.ConnectionId))
        {
            online.Add(person);
        }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnUserLeft(string json)
    {
        var person = Read(json, EasyChatzJsonContext.Default.ChatPresenceDto);
        if (person is not null)
        {
            online.RemoveAll(p => p.ConnectionId == person.ConnectionId);
        }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnHubError(string message)
    {
        error = string.IsNullOrWhiteSpace(message) ? "Something went wrong." : message;
        return Task.CompletedTask;
    }

    private void AddMessage(ChatMessageDto message)
    {
        if (messages.All(m => m.Id != message.Id))
        {
            messages.Add(message);
        }
    }

    private void UpsertRoom(ChatRoomDto room)
    {
        int index = rooms.FindIndex(r => r.Id == room.Id);
        if (index >= 0)
        {
            rooms[index] = room;
        }
        else
        {
            int insertAt = rooms.FindIndex(r => !IsAnnouncements(r));
            rooms.Insert(insertAt < 0 ? rooms.Count : insertAt, room);
        }

        if (activeRoom?.Id == room.Id)
        {
            activeRoom = room;
        }
    }

    private void ApplyRename(ChatRoomUpdatedDto room)
    {
        var dto = new ChatRoomDto(room.Id, room.Name, room.AdminUserId, room.AdminDisplayName);
        UpsertRoom(dto);
    }

    private void RemoveRoom(int roomId)
    {
        rooms.RemoveAll(r => r.Id == roomId);
        roomUnread.Remove(roomId);
        if (activeRoom?.Id == roomId)
        {
            activeRoom = null;
            messages.Clear();
        }
    }

    private void BumpConversation(string userId, string displayName)
    {
        int index = conversations.FindIndex(c => string.Equals(c.UserId, userId, StringComparison.Ordinal));
        if (index < 0)
        {
            conversations.Insert(0, new ChatConversationDto(userId, displayName, 1));
            return;
        }

        var current = conversations[index];
        conversations.RemoveAt(index);
        conversations.Insert(0, current with { DisplayName = displayName, UnreadCount = current.UnreadCount + 1 });
    }

    private void EnsureConversation(string userId, string displayName)
    {
        if (conversations.Any(c => string.Equals(c.UserId, userId, StringComparison.Ordinal)))
        {
            return;
        }

        conversations.Insert(0, new ChatConversationDto(userId, string.IsNullOrWhiteSpace(displayName) ? userId : displayName, 0));
    }

    private void ReplaceConversation(ChatConversationDto conversation)
    {
        int index = conversations.FindIndex(c => string.Equals(c.UserId, conversation.UserId, StringComparison.Ordinal));
        if (index >= 0)
        {
            conversations[index] = conversation;
        }

        if (string.Equals(activeConversation?.UserId, conversation.UserId, StringComparison.Ordinal))
        {
            activeConversation = conversation;
        }
    }

    private async Task PublishConversationAsync(ChatConversationEvent conversation)
    {
        if (module is not null)
        {
            await module.InvokeVoidAsync("raise", root, Constants.DomEvents.Conversation, conversation);
        }

        await OnConversationChanged.InvokeAsync(conversation);
    }

    private static T? Read<T>(string json, JsonTypeInfo<T> type) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize(json, type);

    private static bool IsAnnouncements(ChatRoomDto room) =>
        string.Equals(room.Name, Constants.AnnouncementsRoomName, StringComparison.OrdinalIgnoreCase);

    private static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return char.ToUpperInvariant(parts[0][0]).ToString();
        }

        return string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[^1][0]));
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (module is not null)
            {
                await module.InvokeVoidAsync("disconnect", sessionId);
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        self?.Dispose();
    }
}
