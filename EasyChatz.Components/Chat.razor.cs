using System.Text.Json.Serialization.Metadata;
using EasyChatz.Components.Http;
using Microsoft.AspNetCore.Components.Web;

namespace EasyChatz.Components;

/// <summary>
/// Represents the chat.
/// </summary>
public partial class Chat : ComponentBase, IAsyncDisposable
{
    /// <summary>First page. Usually a little more than a window; a shortfall loads only the rest.</summary>
    private const int FirstPageSize = 15;

    /// <summary>Older pages fetched while scrolling up, and the cap for one viewport top-up.</summary>
    private const int OlderPageSize = 30;
    private const string ConnectingMessage = "Still connecting…";

    private readonly string sessionId = Guid.NewGuid().ToString("N");
    private readonly List<ChatRoomDto> rooms = [];
    private readonly List<ChatConversationDto> conversations = [];
    private readonly Dictionary<int, int> roomUnread = [];
    private readonly List<ChatMessageDto> messages = [];
    private readonly HashSet<int> seenMessageIds = [];
    private readonly List<ChatPresenceDto> online = [];
    private readonly List<ChatMentionDto> pickedMentions = [];
    private readonly HashSet<int> roomMentioned = [];

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
    private bool connecting;
    private bool connectFailed;
    private bool disposed;
    private bool peopleOpen;
    private bool newRoomOpen;
    private bool renameOpen;
    private bool confirmDelete;
    private bool loadingOlder;
    private bool busy;
    private bool scrollPending;
    private bool hasMore;

    /// <summary>The latest page did not fill the window, so the next render should top it up.</summary>
    private bool fillPending;
    private int fillRounds;
    private int threadVersion;
    private int? restoreFromHeight;
    private string filter = string.Empty;
    private string draft = string.Empty;
    private string newRoomName = string.Empty;
    private string renameName = string.Empty;
    private string? error;
    private string? appliedWithUserId;
    private bool showArchived;
    private ChatFeaturesDto features = new(false, false, false);

    [Inject]
    private IChatService ChatService { get; set; } = default!;

    [Inject]
    private IChatNotificationService Notifications { get; set; } = default!;

    [Inject]
    private EasyChatzApiAddress Address { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    /// <summary>
    /// Gets or sets the theme.
    /// </summary>
    [Parameter]
    public string Theme { get; set; } = Constants.Themes.Auto;

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    [Parameter]
    public string Height { get; set; } = "640px";

    /// <summary>
    /// Gets or sets the class.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is show people.
    /// </summary>
    [Parameter]
    public bool ShowPeople { get; set; } = true;

    /// <summary>
    /// Gets or sets the callback raised for the conversation changed event.
    /// </summary>
    [Parameter]
    public EventCallback<ChatConversationEvent> OnConversationChanged { get; set; }

    /// <summary>
    /// Gets or sets the callback raised for the message event.
    /// </summary>
    [Parameter]
    public EventCallback<ChatMessageDto> OnMessage { get; set; }

    /// <summary>Opens this direct message when set, including when the value changes. The user must exist in the host directory.</summary>
    [Parameter]
    public string? WithUserId { get; set; }

    /// <summary>Shown beside Message in the online list when a host is listening. Hidden otherwise.</summary>
    [Parameter]
    public EventCallback<ChatPresenceDto> OnOpenUser { get; set; }

    /// <summary>
    /// Gets or sets the open user text.
    /// </summary>
    [Parameter]
    public string OpenUserText { get; set; } = "Open";

    /// <summary>Caps the composer. The server still enforces <c>EasyChatz:MaxMessageLength</c>.</summary>
    [Parameter]
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the additional attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private int MaxMessageLength => MaxLength is int length and > 0
        ? Math.Min(length, Constants.MaxLengths.Message)
        : Constants.MaxLengths.Message;

    private bool CanSend => !busy && !string.IsNullOrWhiteSpace(draft) && !ReadOnlyThread;

    private bool ReadOnlyThread =>
        (features.AllowRoomArchive && activeRoom is { IsArchived: true })
        || (features.AllowUserBlocks && activeConversation is { HasBlockedMe: true })
        || (activeRoom is not null && IsAnnouncements(activeRoom) && me?.IsAdministrator != true);

    private string ReadOnlyHint =>
        features.AllowRoomArchive && activeRoom is { IsArchived: true }
            ? "This room is archived. Nobody can post."
            : features.AllowUserBlocks && activeConversation is { HasBlockedMe: true }
                ? "You can't message this person."
                : $"Only an administrator can post in {activeRoom?.Name}.";

    private bool CanRename => activeRoom is not null && me is not null && !IsAnnouncements(activeRoom)
        && string.Equals(activeRoom.AdminUserId, me.Id, StringComparison.Ordinal);

    private bool CanDeleteRoom => activeRoom is not null && me is not null && !IsAnnouncements(activeRoom)
        && (me.IsAdministrator || string.Equals(activeRoom.AdminUserId, me.Id, StringComparison.Ordinal));

    private bool CanArchiveRoom => features.AllowRoomArchive && activeRoom is not null && me is not null && !IsAnnouncements(activeRoom)
        && (me.IsAdministrator || string.Equals(activeRoom.AdminUserId, me.Id, StringComparison.Ordinal));

    private bool CanBlock => features.AllowUserBlocks && activeConversation is not null && me is not null
        && !string.Equals(activeConversation.UserId, me.Id, StringComparison.Ordinal);

    private bool CanDeleteMessage(ChatMessageDto message) =>
        features.AllowMessageDelete
        && !message.IsDeleted
        && me is not null
        && string.Equals(message.FromUserId, me.Id, StringComparison.Ordinal);

    private IEnumerable<ChatRoomDto> VisibleRooms
    {
        get
        {
            IEnumerable<ChatRoomDto> source = features.AllowRoomArchive && !showArchived
                ? rooms.Where(r => !r.IsArchived)
                : rooms;
            if (string.IsNullOrWhiteSpace(filter))
            {
                return source;
            }

            string term = filter.Trim();
            return source.Where(r => r.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }

    private IEnumerable<ChatConversationDto> VisibleConversations => string.IsNullOrWhiteSpace(filter)
        ? conversations
        : conversations.Where(c => c.DisplayName.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase));

    private IEnumerable<ChatPresenceDto> OtherPeople => online
        .Where(p => me is null || !string.Equals(p.UserId, me.Id, StringComparison.Ordinal))
        .GroupBy(p => p.UserId, StringComparer.Ordinal)
        .Select(g => g.First());

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var current = await ChatService.GetCurrentUserAsync();
        if (!current.IsSuccess)
        {
            error = current.GetDisplayMessage();
            return;
        }

        var featureResult = await ChatService.GetFeaturesAsync();
        if (featureResult.IsSuccess && featureResult.Value is not null)
        {
            features = featureResult.Value;
        }

        me = current.Value;
        await ReloadListsAsync();
        if (!string.IsNullOrWhiteSpace(WithUserId))
        {
            appliedWithUserId = WithUserId;
            await OpenWithUserAsync(WithUserId);
        }
        else if (rooms.Count > 0)
        {
            await OpenRoomAsync(rooms[0]);
        }
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (me is null || string.IsNullOrWhiteSpace(WithUserId)
            || string.Equals(appliedWithUserId, WithUserId, StringComparison.Ordinal))
        {
            return;
        }

        appliedWithUserId = WithUserId;
        await OpenWithUserAsync(WithUserId);
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // WebAssembly renders once before OnInitializedAsync finishes, so the signed-in user
        // is often missing on the first render. Connect on the first render that has one.
        if (disposed || me is null)
        {
            return;
        }

        if (module is null && !await ImportModuleAsync())
        {
            return;
        }

        if (!composerBound && !ReadOnlyThread && (activeRoom is not null || activeConversation is not null))
        {
            composerBound = await TryBindComposerAsync();
        }

        if (scrollPending)
        {
            scrollPending = false;
            await InvokeQuietlyAsync("scrollToEnd", scroller);
        }

        if (restoreFromHeight is int previous)
        {
            restoreFromHeight = null;
            await InvokeQuietlyAsync("restoreHeight", scroller, previous);
        }

        // A page that doesn't overflow can never be scrolled, so the scroll handler would never
        // ask for the rest. Measure after layout and fetch only what the window is still short.
        if (fillPending && !loadingOlder && hasMore && fillRounds < 6 && messages.Count > 0 && module is not null)
        {
            fillPending = false;
            int version = threadVersion;
            int shortfall = await module.InvokeAsync<int>("shortfall", scroller);
            if (version == threadVersion && shortfall > 0)
            {
                int average = Math.Max(1, await module.InvokeAsync<int>("averageItemHeight", scroller));
                if (version == threadVersion)
                {
                    fillRounds++;
                    int take = Math.Clamp((int)Math.Ceiling(shortfall / (double)average), 1, OlderPageSize);
                    await LoadMessagesAsync(messages[0].Id, older: true, take, filling: true);
                    if (version == threadVersion)
                    {
                        StateHasChanged();
                    }
                }
            }
        }

        if (!connected && !connecting && !connectFailed)
        {
            await ConnectAsync();
        }
    }

    private async Task<bool> ImportModuleAsync()
    {
        try
        {
            module = await Js.InvokeAsync<IJSObjectReference>("import", "./_content/EasyChatz.Components/easychatz.js");
            self ??= DotNetObjectReference.Create(this);
            return module is not null;
        }
        catch (Exception ex) when (IsBrowserGone(ex))
        {
            return false;
        }
        catch (JSException)
        {
            connectFailed = true;
            return false;
        }
    }

    private async Task<bool> TryBindComposerAsync()
    {
        if (module is null || self is null)
        {
            return false;
        }

        try
        {
            return await module.InvokeAsync<bool>("bindComposer", composer, self);
        }
        catch (Exception ex) when (IsBrowserGone(ex))
        {
            return false;
        }
    }

    private async Task ConnectAsync()
    {
        if (disposed || module is null || self is null || connected || connecting)
        {
            return;
        }

        connecting = true;
        try
        {
            await module.InvokeVoidAsync("connect", sessionId, Address.HubUrl, Address.IncludeCredentials, self);
            connected = true;
            connectFailed = false;
            bool clearError = error == ConnectingMessage;
            if (clearError)
            {
                error = null;
            }

            if (activeRoom is not null)
            {
                await InvokeQuietlyAsync("join", sessionId, activeRoom.Name);
                await PublishConversationAsync(new ChatConversationEvent("room", activeRoom.Id.ToString(), activeRoom.Name));
            }
            else if (activeConversation is not null)
            {
                await PublishConversationAsync(new ChatConversationEvent("private", activeConversation.UserId, activeConversation.DisplayName));
            }

            if (clearError)
            {
                await RenderAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
            connectFailed = true;
        }
        catch (Exception ex) when (IsBrowserGone(ex))
        {
        }
        finally
        {
            connecting = false;
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

        await LoadRoomMentionsAsync();
    }

    /// <summary>Puts a badge on each room that mentioned the user before this page was open.</summary>
    private async Task LoadRoomMentionsAsync()
    {
        var mentions = await Notifications.GetUnreadRoomMentionsAsync();
        if (!mentions.IsSuccess || mentions.Value is null)
        {
            return;
        }

        foreach (var (roomId, count) in mentions.Value)
        {
            if (count <= 0 || activeRoom?.Id == roomId)
            {
                continue;
            }

            roomUnread[roomId] = count;
            roomMentioned.Add(roomId);
        }
    }

    private async Task ReloadConversationsAsync()
    {
        var conversationResult = await ChatService.GetConversationsAsync();
        if (!conversationResult.IsSuccess || conversationResult.Value is null)
        {
            return;
        }

        var onlyLocal = conversations
            .Where(c => conversationResult.Value.All(saved => !string.Equals(saved.UserId, c.UserId, StringComparison.Ordinal)))
            .ToList();
        conversations.Clear();
        conversations.AddRange(conversationResult.Value);
        conversations.AddRange(onlyLocal);
    }

    private async Task OpenRoomAsync(ChatRoomDto room)
    {
        activeRoom = room;
        activeConversation = null;
        composerBound = false;
        roomUnread.Remove(room.Id);
        roomMentioned.Remove(room.Id);
        error = null;
        ResetThread();
        await Notifications.MarkRoomReadAsync(room.Id);
        await LoadMessagesAsync(beforeId: null, older: false);
        if (connected && module is not null)
        {
            await InvokeQuietlyAsync("join", sessionId, room.Name);
        }

        await ReloadConversationsAsync();
        await PublishConversationAsync(new ChatConversationEvent("room", room.Id.ToString(), room.Name));
    }

    private async Task OpenConversationAsync(ChatConversationDto conversation)
    {
        activeConversation = conversation;
        activeRoom = null;
        composerBound = false;
        error = null;
        ResetThread();
        await Notifications.MarkReadAsync(conversation.UserId);
        await ReloadConversationsAsync();
        var saved = conversations.FirstOrDefault(c => string.Equals(c.UserId, conversation.UserId, StringComparison.Ordinal)) ?? conversation;
        activeConversation = saved with { UnreadCount = 0 };
        ReplaceConversation(activeConversation);
        await LoadMessagesAsync(beforeId: null, older: false);
        await PublishConversationAsync(new ChatConversationEvent("private", conversation.UserId, conversation.DisplayName));
    }

    private async Task OpenWithUserAsync(string userId)
    {
        var result = await ChatService.GetConversationAsync(userId);
        if (!result.IsSuccess || result.Value is null)
        {
            error = result.GetDisplayMessage();
            return;
        }

        if (conversations.All(c => !string.Equals(c.UserId, result.Value.UserId, StringComparison.Ordinal)))
        {
            conversations.Insert(0, result.Value);
        }

        await OpenConversationAsync(result.Value);
    }

    private async Task MessagePersonAsync(ChatPresenceDto person)
    {
        peopleOpen = false;
        await OpenWithUserAsync(person.UserId);
    }

    private void ResetThread()
    {
        threadVersion++;
        fillRounds = 0;
        fillPending = false;
        messages.Clear();
    }

    private Task LoadMessagesAsync(int? beforeId, bool older) =>
        LoadMessagesAsync(beforeId, older, older ? OlderPageSize : FirstPageSize, filling: false);

    private async Task LoadMessagesAsync(int? beforeId, bool older, int take, bool filling)
    {
        int version = threadVersion;
        if (older)
        {
            loadingOlder = true;
            // Scrolling up should keep the same messages on screen. Filling a short window should
            // stay on the latest ones, so that path scrolls to the end instead of restoring.
            if (!filling && module is not null)
            {
                restoreFromHeight = await module.InvokeAsync<int>("captureHeight", scroller);
            }
        }

        Result<IReadOnlyList<ChatMessageDto>> result = activeRoom is not null
            ? await ChatService.GetRoomMessagesAsync(activeRoom.Id, take, beforeId)
            : activeConversation is not null
                ? await ChatService.GetPrivateMessagesAsync(activeConversation.UserId, take, beforeId)
                : Result<IReadOnlyList<ChatMessageDto>>.Success([]);

        if (version != threadVersion)
        {
            loadingOlder = false;
            return;
        }

        loadingOlder = false;
        if (!result.IsSuccess || result.Value is null)
        {
            error = result.GetDisplayMessage();
            return;
        }

        hasMore = result.Value.Count >= take;
        if (older)
        {
            var fresh = result.Value.Where(m => messages.All(existing => existing.Id != m.Id)).ToList();
            messages.InsertRange(0, fresh);
            if (filling && fresh.Count > 0)
            {
                scrollPending = true;
                fillPending = hasMore;
            }
        }
        else
        {
            messages.Clear();
            messages.AddRange(result.Value);
            scrollPending = true;
            fillPending = hasMore;
        }
    }

    private async Task OnScrollAsync()
    {
        if (module is null || loadingOlder || fillPending || !hasMore || messages.Count == 0)
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

        // Mentions picked from the list become stored tokens. Only rooms support them.
        string text = activeRoom is not null ? ChatMentions.Encode(draft, pickedMentions) : draft;
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
            pickedMentions.Clear();
            AddMessage(sent.Value);
            scrollPending = true;
            await OnMessage.InvokeAsync(sent.Value);
            return;
        }

        if (module is null)
        {
            connectFailed = false;
            await ImportModuleAsync();
        }

        if (!connected)
        {
            connectFailed = false;
            await ConnectAsync();
        }

        if (activeConversation is null || module is null || !connected)
        {
            busy = false;
            error = activeConversation is null ? "Choose a conversation first." : ConnectingMessage;
            return;
        }

        try
        {
            string? payload = await module.InvokeAsync<string?>("sendPrivate", sessionId, activeConversation.UserId, text);
            draft = string.Empty;
            pickedMentions.Clear();
            var sent = Read(payload, EasyChatzJsonContext.Default.ChatMessageDto);
            if (sent is null)
            {
                error ??= "The message could not be sent.";
                return;
            }

            AddMessage(sent);
            scrollPending = true;
            EnsureConversation(activeConversation.UserId, activeConversation.DisplayName);
            await OnMessage.InvokeAsync(sent);
        }
        catch (Exception ex) when (ex is JSException or OperationCanceledException)
        {
            if (!disposed)
            {
                error = "The message could not be sent.";
            }
        }
        finally
        {
            busy = false;
        }
    }

    /// <summary>
    /// Sends from the composer.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public async Task SendFromComposer(string? text)
    {
        if (text is not null)
        {
            draft = text;
        }

        await SendAsync();
        await RenderAsync();
    }

    /// <summary>Called by the composer while the user types <c>@name</c>. Only rooms offer suggestions.</summary>
    [JSInvokable]
    public async Task<string> SearchMentions(string? term)
    {
        if (activeRoom is null || me is null)
        {
            return "[]";
        }

        var found = await ChatService.SearchMentionsAsync(term, 6);
        return JsonSerializer.Serialize(
            found.IsSuccess && found.Value is not null ? found.Value.ToList() : [],
            EasyChatzJsonContext.Default.ListChatMentionDto);
    }

    /// <summary>Remembers who was picked from the list, so their <c>@name</c> is stored as a real mention on send.</summary>
    [JSInvokable]
    public void RegisterMention(string userId, string displayName)
    {
        if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(displayName))
        {
            pickedMentions.RemoveAll(m => string.Equals(m.UserId, userId, StringComparison.Ordinal));
            pickedMentions.Add(new ChatMentionDto(userId, displayName));
        }
    }

    private bool MentionsMe(ChatMessageDto message) =>
        me is not null
        && message.ToRoomId is not null
        && !string.Equals(message.FromUserId, me.Id, StringComparison.Ordinal)
        && ChatMentions.Mentions(message.Content, me.Id);

    /// <summary>
    /// Built by hand rather than in markup: the bubble keeps line breaks (<c>white-space: pre-wrap</c>),
    /// so any whitespace between Razor elements would show up as stray spaces.
    /// </summary>
    private RenderFragment RenderContent(ChatMessageDto message) => builder =>
    {
        // Direct messages never contain mentions. A token typed by hand there is shown as text.
        if (message.ToRoomId is null)
        {
            builder.AddContent(0, message.Content);
            return;
        }

        foreach (var part in ChatMentions.Parse(message.Content))
        {
            if (part.MentionUserId is null)
            {
                builder.AddContent(0, part.Text);
                continue;
            }

            bool self = me is not null && string.Equals(part.MentionUserId, me.Id, StringComparison.Ordinal);
            builder.OpenElement(1, "span");
            builder.AddAttribute(2, "class", self ? "ec-mention ec-mention-me" : "ec-mention");
            builder.AddContent(3, "@" + part.Text);
            builder.CloseElement();
        }
    };

    private Task OnComposerKeyDown(KeyboardEventArgs args) =>
        args.Key == "Enter" && !args.ShiftKey ? SendFromComposer(null) : Task.CompletedTask;

    private Task OnDraft(ChangeEventArgs args)
    {
        draft = args.Value?.ToString() ?? string.Empty;
        return Task.CompletedTask;
    }

    private async Task DeleteMessageAsync(int messageId)
    {
        var deleted = await ChatService.DeleteMessageAsync(messageId);
        if (!deleted.IsSuccess || deleted.Value is null)
        {
            error = deleted.GetDisplayMessage();
            return;
        }

        ReplaceMessage(deleted.Value);
    }

    private async Task ToggleArchiveAsync()
    {
        if (activeRoom is null)
        {
            return;
        }

        var result = activeRoom.IsArchived
            ? await ChatService.UnarchiveRoomAsync(activeRoom.Id)
            : await ChatService.ArchiveRoomAsync(activeRoom.Id);
        if (!result.IsSuccess || result.Value is null)
        {
            error = result.GetDisplayMessage();
            return;
        }

        error = null;
        composerBound = false;
        UpsertRoom(result.Value);
    }

    private async Task ToggleBlockAsync()
    {
        if (activeConversation is null)
        {
            return;
        }

        var result = activeConversation.BlockedByMe
            ? await ChatService.UnblockUserAsync(activeConversation.UserId)
            : await ChatService.BlockUserAsync(activeConversation.UserId);
        if (!result.IsSuccess || result.Value is null)
        {
            error = result.GetDisplayMessage();
            return;
        }

        error = null;
        composerBound = false;
        ReplaceConversation(result.Value);
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

    /// <summary>
    /// Handles the room message event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public async Task OnRoomMessage(string json)
    {
        var message = Read(json, EasyChatzJsonContext.Default.ChatMessageDto);
        if (message is null)
        {
            return;
        }

        if (!seenMessageIds.Add(message.Id))
        {
            return;
        }

        if (activeRoom is not null && message.ToRoomId == activeRoom.Id)
        {
            AddMessage(message);
            scrollPending = true;
            if (me is not null && ChatMentions.Mentions(message.Content, me.Id))
            {
                await Notifications.MarkRoomReadAsync(activeRoom.Id);
            }
        }
        else if (message.ToRoomId is int roomId && !string.Equals(message.FromUserId, me?.Id, StringComparison.Ordinal))
        {
            roomUnread[roomId] = roomUnread.GetValueOrDefault(roomId) + 1;
            if (me is not null && ChatMentions.Mentions(message.Content, me.Id))
            {
                roomMentioned.Add(roomId);
            }
        }

        await OnMessage.InvokeAsync(message);
        await RenderAsync();
    }

    /// <summary>
    /// Handles the private message event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public async Task OnPrivateMessage(string json)
    {
        var message = Read(json, EasyChatzJsonContext.Default.ChatMessageDto);
        if (message is null || me is null)
        {
            return;
        }

        if (!seenMessageIds.Add(message.Id))
        {
            return;
        }

        string partnerId = string.Equals(message.FromUserId, me.Id, StringComparison.Ordinal)
            ? message.ToUserId ?? string.Empty
            : message.FromUserId;
        bool mine = string.Equals(message.FromUserId, me.Id, StringComparison.Ordinal);
        bool open = activeConversation is not null && string.Equals(activeConversation.UserId, partnerId, StringComparison.Ordinal);
        if (open)
        {
            AddMessage(message);
            scrollPending = true;
            if (!mine)
            {
                ClearUnread(partnerId);
                await Notifications.MarkReadAsync(partnerId);
            }
        }
        else if (!mine)
        {
            BumpConversation(partnerId, message.FromDisplayName);
        }
        else
        {
            EnsureConversation(partnerId, activeConversation?.DisplayName ?? partnerId);
        }

        await OnMessage.InvokeAsync(message);
        await RenderAsync();
    }

    /// <summary>
    /// Handles the message removed event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnMessageRemoved(string json)
    {
        var removed = Read(json, EasyChatzJsonContext.Default.ChatMessageRemovedDto);
        if (removed is not null)
        {
            messages.RemoveAll(m => m.Id == removed.MessageId);
            return RenderAsync();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the message deleted event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnMessageDeleted(string json)
    {
        var message = Read(json, EasyChatzJsonContext.Default.ChatMessageDto);
        if (message is null || !IsOpenThread(message))
        {
            return Task.CompletedTask;
        }

        ReplaceMessage(message);
        return RenderAsync();
    }

    /// <summary>
    /// Handles the room archived event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnRoomArchived(string json)
    {
        var room = Read(json, EasyChatzJsonContext.Default.ChatRoomDto);
        if (room is null)
        {
            return Task.CompletedTask;
        }

        if (activeRoom?.Id == room.Id)
        {
            composerBound = false;
        }

        UpsertRoom(room);
        return RenderAsync();
    }

    /// <summary>
    /// Handles the user blocked event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnUserBlocked(string json)
    {
        var change = Read(json, EasyChatzJsonContext.Default.ChatBlockChangedDto);
        if (change is null || me is null)
        {
            return Task.CompletedTask;
        }

        if (string.Equals(change.BlockerUserId, me.Id, StringComparison.Ordinal))
        {
            UpdateBlock(change.BlockedUserId, blockedByMe: change.IsBlocked, hasBlockedMe: null);
            return RenderAsync();
        }

        if (string.Equals(change.BlockedUserId, me.Id, StringComparison.Ordinal))
        {
            UpdateBlock(change.BlockerUserId, blockedByMe: null, hasBlockedMe: change.IsBlocked);
            return RenderAsync();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the room added event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnRoomAdded(string json)
    {
        var room = Read(json, EasyChatzJsonContext.Default.ChatRoomDto);
        if (room is not null)
        {
            UpsertRoom(room);
            return RenderAsync();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the room updated event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
            await InvokeQuietlyAsync("join", sessionId, room.Name);
        }

        await RenderAsync();
    }

    /// <summary>
    /// Handles the room removed event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnRoomRemoved(string json)
    {
        var room = Read(json, EasyChatzJsonContext.Default.ChatRoomRemovedDto);
        if (room is not null)
        {
            RemoveRoom(room.RoomId);
            return RenderAsync();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the room deleted event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnRoomDeleted(string json) => OnRoomRemoved(json);

    /// <summary>
    /// Handles the online users event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnOnlineUsers(string json)
    {
        online.Clear();
        var people = Read(json, EasyChatzJsonContext.Default.ListChatPresenceDto);
        if (people is not null)
        {
            online.AddRange(people);
        }

        return RenderAsync();
    }

    /// <summary>
    /// Handles the user joined event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnUserJoined(string json)
    {
        var person = Read(json, EasyChatzJsonContext.Default.ChatPresenceDto);
        if (person is not null && online.All(p => p.ConnectionId != person.ConnectionId))
        {
            online.Add(person);
            return RenderAsync();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the user left event.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnUserLeft(string json)
    {
        var person = Read(json, EasyChatzJsonContext.Default.ChatPresenceDto);
        if (person is not null)
        {
            online.RemoveAll(p => p.ConnectionId == person.ConnectionId);
            return RenderAsync();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the hub error event.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnHubError(string message)
    {
        error = string.IsNullOrWhiteSpace(message) ? "Something went wrong." : message;
        return RenderAsync();
    }

    private void AddMessage(ChatMessageDto message)
    {
        seenMessageIds.Add(message.Id);
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
        bool archived = rooms.FirstOrDefault(r => r.Id == room.Id)?.IsArchived
            ?? (activeRoom?.Id == room.Id && activeRoom.IsArchived);
        var dto = new ChatRoomDto(room.Id, room.Name, room.AdminUserId, room.AdminDisplayName, archived);
        UpsertRoom(dto);
    }

    private void RemoveRoom(int roomId)
    {
        rooms.RemoveAll(r => r.Id == roomId);
        roomUnread.Remove(roomId);
        if (activeRoom?.Id == roomId)
        {
            activeRoom = null;
            ResetThread();
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

    private void ClearUnread(string userId)
    {
        int index = conversations.FindIndex(c => string.Equals(c.UserId, userId, StringComparison.Ordinal));
        if (index < 0 || conversations[index].UnreadCount == 0)
        {
            return;
        }

        var cleared = conversations[index] with { UnreadCount = 0 };
        conversations[index] = cleared;
        if (string.Equals(activeConversation?.UserId, userId, StringComparison.Ordinal))
        {
            activeConversation = cleared;
        }
    }

    private void ReplaceMessage(ChatMessageDto message)
    {
        int index = messages.FindIndex(m => m.Id == message.Id);
        if (index >= 0)
        {
            messages[index] = message;
        }
    }

    private void UpdateBlock(string userId, bool? blockedByMe, bool? hasBlockedMe)
    {
        int index = conversations.FindIndex(c => string.Equals(c.UserId, userId, StringComparison.Ordinal));
        if (index < 0)
        {
            return;
        }

        var current = conversations[index];
        var updated = current with
        {
            BlockedByMe = blockedByMe ?? current.BlockedByMe,
            HasBlockedMe = hasBlockedMe ?? current.HasBlockedMe,
        };
        conversations[index] = updated;
        if (string.Equals(activeConversation?.UserId, userId, StringComparison.Ordinal))
        {
            composerBound = false;
            activeConversation = updated;
        }
    }

    private bool IsOpenThread(ChatMessageDto message)
    {
        if (activeRoom is not null)
        {
            return message.ToRoomId == activeRoom.Id;
        }

        if (activeConversation is null || me is null)
        {
            return false;
        }

        bool fromMe = string.Equals(message.FromUserId, me.Id, StringComparison.Ordinal)
            && string.Equals(message.ToUserId, activeConversation.UserId, StringComparison.Ordinal);
        bool toMe = string.Equals(message.FromUserId, activeConversation.UserId, StringComparison.Ordinal)
            && string.Equals(message.ToUserId, me.Id, StringComparison.Ordinal);
        return fromMe || toMe;
    }

    private static string DeletedLabel(ChatMessageDto message)
    {
        string user = string.IsNullOrWhiteSpace(message.DeletedByDisplayName) ? "someone" : message.DeletedByDisplayName;
        if (message.DeletedAtUtc is not DateTime deletedAt)
        {
            return $"Message deleted by {user}";
        }

        return $"Message deleted by {user} at {deletedAt.ToLocalTime():g}";
    }

    private string RoomSubtitle(ChatRoomDto room) =>
        features.AllowRoomArchive && room.IsArchived
            ? $"Archived · Started by {room.AdminDisplayName}"
            : $"Started by {room.AdminDisplayName}";

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
        // The DOM event is for MVC and React. A canceled browser call must not fail the click that opened the chat.
        await InvokeQuietlyAsync(
            "raise",
            root,
            Constants.DomEvents.Conversation,
            conversation.Kind,
            conversation.Id,
            conversation.Name);
        await OnConversationChanged.InvokeAsync(conversation);
    }

    private async Task<bool> InvokeQuietlyAsync(string identifier, params object?[] args)
    {
        if (disposed || module is null)
        {
            return false;
        }

        try
        {
            await module.InvokeVoidAsync(identifier, args);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task RenderAsync()
    {
        if (disposed)
        {
            return;
        }

        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex) when (IsBrowserGone(ex))
        {
        }
    }

    private static bool IsBrowserGone(Exception ex) =>
        ex is JSDisconnectedException or JSException or OperationCanceledException or ObjectDisposedException;

    private static T? Read<T>(string? json, JsonTypeInfo<T> type) =>
        string.IsNullOrWhiteSpace(json) || json == "null" ? default : JsonSerializer.Deserialize(json, type);

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

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        disposed = true;
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
