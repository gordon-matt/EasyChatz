namespace EasyChatz.Components.Http;

internal sealed class HttpChatService(EasyChatzApiClient api) : IChatService
{
    private static EasyChatzJsonContext Json => EasyChatzJsonContext.Default;

    public Task<Result<ChatUser>> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync(ApiRoutes.Me, Json.ChatUser, cancellationToken);

    public Task<Result<ChatFeaturesDto>> GetFeaturesAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync(ApiRoutes.Features, Json.ChatFeaturesDto, cancellationToken);

    public async Task<Result<IReadOnlyList<ChatRoomDto>>> GetRoomsAsync(CancellationToken cancellationToken = default) =>
        (await api.GetAsync(ApiRoutes.Rooms, Json.ListChatRoomDto, cancellationToken)).MapList();

    public Task<Result<ChatRoomDto>> GetRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        api.GetAsync($"rooms/{roomId}", Json.ChatRoomDto, cancellationToken);

    public Task<Result<ChatRoomDto>> CreateRoomAsync(string name, CancellationToken cancellationToken = default) =>
        api.SendAsync(HttpMethod.Post, ApiRoutes.Rooms, new SaveRoomRequest(name), Json.SaveRoomRequest, Json.ChatRoomDto, cancellationToken);

    public Task<Result<ChatRoomUpdatedDto>> UpdateRoomAsync(int roomId, string name, CancellationToken cancellationToken = default) =>
        api.SendAsync(HttpMethod.Put, $"rooms/{roomId}", new SaveRoomRequest(name), Json.SaveRoomRequest, Json.ChatRoomUpdatedDto, cancellationToken);

    public Task<Result<ChatRoomRemovedDto>> DeleteRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        api.DeleteAsync($"rooms/{roomId}", Json.ChatRoomRemovedDto, cancellationToken);

    public Task<Result<ChatRoomDto>> ArchiveRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        api.SendAsync(HttpMethod.Post, $"rooms/{roomId}/archive", Json.ChatRoomDto, cancellationToken);

    public Task<Result<ChatRoomDto>> UnarchiveRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        api.DeleteAsync($"rooms/{roomId}/archive", Json.ChatRoomDto, cancellationToken);

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> GetRoomMessagesAsync(int roomId, int? take = null, int? beforeId = null, CancellationToken cancellationToken = default) =>
        (await api.GetAsync(Page($"rooms/{roomId}/messages", take, beforeId), Json.ListChatMessageDto, cancellationToken)).MapList();

    public Task<Result<ChatMessageDto>> SendRoomMessageAsync(int roomId, string content, CancellationToken cancellationToken = default) =>
        api.SendAsync(HttpMethod.Post, $"rooms/{roomId}/messages", new SendMessageRequest(content), Json.SendMessageRequest, Json.ChatMessageDto, cancellationToken);

    public async Task<Result<IReadOnlyList<ChatConversationDto>>> GetConversationsAsync(CancellationToken cancellationToken = default) =>
        (await api.GetAsync(ApiRoutes.Conversations, Json.ListChatConversationDto, cancellationToken)).MapList();

    public Task<Result<ChatConversationDto>> GetConversationAsync(string userId, CancellationToken cancellationToken = default) =>
        api.GetAsync($"conversations/{Uri.EscapeDataString(userId)}", Json.ChatConversationDto, cancellationToken);

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> GetPrivateMessagesAsync(string otherUserId, int? take = null, int? beforeId = null, CancellationToken cancellationToken = default) =>
        (await api.GetAsync(Page($"conversations/{Uri.EscapeDataString(otherUserId)}/messages", take, beforeId), Json.ListChatMessageDto, cancellationToken)).MapList();

    public Task<Result<ChatMessageDto>> SendPrivateMessageAsync(string toUserId, string content, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<ChatMessageDto>.Error("Direct messages are sent through the chat hub."));

    public Task<Result<ChatConversationDto>> BlockUserAsync(string userId, CancellationToken cancellationToken = default) =>
        api.SendAsync(HttpMethod.Post, $"conversations/{Uri.EscapeDataString(userId)}/block", Json.ChatConversationDto, cancellationToken);

    public Task<Result<ChatConversationDto>> UnblockUserAsync(string userId, CancellationToken cancellationToken = default) =>
        api.DeleteAsync($"conversations/{Uri.EscapeDataString(userId)}/block", Json.ChatConversationDto, cancellationToken);

    public Task<Result<ChatMessageDto>> DeleteMessageAsync(int messageId, CancellationToken cancellationToken = default) =>
        api.DeleteAsync($"messages/{messageId}", Json.ChatMessageDto, cancellationToken);

    public async Task<Result<IReadOnlyList<ChatMentionDto>>> SearchMentionsAsync(string? term, int take = 8, CancellationToken cancellationToken = default) =>
        (await api.GetAsync($"{ApiRoutes.Mentions}?q={Uri.EscapeDataString(term ?? string.Empty)}&take={take}", Json.ListChatMentionDto, cancellationToken)).MapList();

    public async Task<Result<IReadOnlyList<ChatMentionDto>>> SearchPeopleAsync(string? term, int take = 8, CancellationToken cancellationToken = default) =>
        (await api.GetAsync($"people?q={Uri.EscapeDataString(term ?? string.Empty)}&take={take}", Json.ListChatMentionDto, cancellationToken)).MapList();

    public Task<Result<ChatConversationDto>> RequestConnectionAsync(string userId, string? introduction, CancellationToken cancellationToken = default) =>
        api.SendAsync(HttpMethod.Post, $"conversations/{Uri.EscapeDataString(userId)}/connection", new ConnectionRequest(introduction), Json.ConnectionRequest, Json.ChatConversationDto, cancellationToken);

    public Task<Result<ChatConversationDto>> AcceptConnectionAsync(string userId, CancellationToken cancellationToken = default) =>
        api.SendAsync(HttpMethod.Post, $"conversations/{Uri.EscapeDataString(userId)}/connection/accept", Json.ChatConversationDto, cancellationToken);

    public Task<Result<ChatConversationDto>> DeclineConnectionAsync(string userId, CancellationToken cancellationToken = default) =>
        api.SendAsync(HttpMethod.Post, $"conversations/{Uri.EscapeDataString(userId)}/connection/decline", Json.ChatConversationDto, cancellationToken);

    public async Task<Result<IReadOnlyList<string>>> GetConnectedUserIdsAsync(CancellationToken cancellationToken = default)
    {
        var conversations = await GetConversationsAsync(cancellationToken);
        return !conversations.IsSuccess || conversations.Value is null
            ? conversations.To<IReadOnlyList<string>>()
            : Result<IReadOnlyList<string>>.Success(conversations.Value
                .Where(item => item.Connection == ChatConnectionStatus.Accepted)
                .Select(item => item.UserId)
                .ToList());
    }

    public async Task<Result<int>> CountIncomingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var conversations = await GetConversationsAsync(cancellationToken);
        return !conversations.IsSuccess || conversations.Value is null
            ? conversations.To<int>()
            : Result<int>.Success(conversations.Value.Count(item => item.Connection == ChatConnectionStatus.PendingIncoming));
    }

    private static string Page(string path, int? take, int? beforeId)
    {
        var query = new List<string>();
        if (take is not null)
        {
            query.Add($"take={take.Value}");
        }

        if (beforeId is not null)
        {
            query.Add($"beforeId={beforeId.Value}");
        }

        return query.Count == 0 ? path : path + "?" + string.Join('&', query);
    }
}

internal sealed class HttpChatNotificationService(EasyChatzApiClient api) : IChatNotificationService
{
    public Task CreateAsync(int chatMessageId, string fromUserId, string toUserId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public async Task<Result<IReadOnlyList<ChatConversationDto>>> GetUnreadAsync(CancellationToken cancellationToken = default) =>
        (await api.GetAsync(ApiRoutes.Notifications, EasyChatzJsonContext.Default.ListChatConversationDto, cancellationToken)).MapList();

    public Task<Result> MarkReadAsync(string fromUserId, CancellationToken cancellationToken = default) =>
        api.SendAsync(HttpMethod.Post, $"notifications/{Uri.EscapeDataString(fromUserId)}/read", cancellationToken);
}

internal static class ResultListExtensions
{
    public static Result<IReadOnlyList<T>> MapList<T>(this Result<List<T>> result) =>
        result.IsSuccess
            ? Result<IReadOnlyList<T>>.Success(result.Value)
            : result.To<IReadOnlyList<T>>();
}
