using EasyChatz.Models;

namespace EasyChatz.Services;

public interface IChatService
{
    Task<Result<ChatUser>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ChatRoomDto>>> GetRoomsAsync(CancellationToken cancellationToken = default);

    Task<Result<ChatRoomDto>> GetRoomAsync(int roomId, CancellationToken cancellationToken = default);

    Task<Result<ChatRoomDto>> CreateRoomAsync(string name, CancellationToken cancellationToken = default);

    Task<Result<ChatRoomUpdatedDto>> UpdateRoomAsync(int roomId, string name, CancellationToken cancellationToken = default);

    Task<Result<ChatRoomRemovedDto>> DeleteRoomAsync(int roomId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ChatMessageDto>>> GetRoomMessagesAsync(int roomId, int? take = null, int? beforeId = null, CancellationToken cancellationToken = default);

    Task<Result<ChatMessageDto>> SendRoomMessageAsync(int roomId, string content, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ChatConversationDto>>> GetConversationsAsync(CancellationToken cancellationToken = default);

    Task<Result<ChatConversationDto>> GetConversationAsync(string userId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ChatMessageDto>>> GetPrivateMessagesAsync(string otherUserId, int? take = null, int? beforeId = null, CancellationToken cancellationToken = default);

    Task<Result<ChatMessageDto>> SendPrivateMessageAsync(string toUserId, string content, CancellationToken cancellationToken = default);

    Task<Result<ChatMessageRemovedDto>> DeleteMessageAsync(int messageId, CancellationToken cancellationToken = default);

    /// <summary>People matching <paramref name="term"/> who can be mentioned in a room. The signed-in user is left out.</summary>
    Task<Result<IReadOnlyList<ChatMentionDto>>> SearchMentionsAsync(string? term, int take = 8, CancellationToken cancellationToken = default);
}

public interface IChatNotificationService
{
    Task CreateAsync(int chatMessageId, string fromUserId, string toUserId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ChatConversationDto>>> GetUnreadAsync(CancellationToken cancellationToken = default);

    Task<Result> MarkReadAsync(string fromUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Unread direct messages waiting for a host-owned channel. Call this from a trusted background job,
/// not from a page: it can see every recipient.
/// </summary>
public interface IChatDeliveryQueue
{
    Task<IReadOnlyList<ChatPendingNotice>> ListPendingAsync(DateTime createdBeforeUtc, CancellationToken cancellationToken = default);

    Task MarkDeliveredAsync(IReadOnlyCollection<int> noticeIds, CancellationToken cancellationToken = default);
}

/// <summary>The host's user store. EasyChatz never authenticates; it only asks who someone is.</summary>
public interface IChatUserDirectory
{
    Task<ChatUser?> FindAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, ChatUser>> FindManyAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// People whose name contains <paramref name="term"/>, for <c>@</c> mentions. Hosts that do not
    /// implement this get no suggestions, and mentions stay switched off.
    /// </summary>
    Task<IReadOnlyList<ChatUser>> SearchAsync(string term, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ChatUser>>([]);
}

/// <summary>The signed-in user for the current request or Blazor circuit.</summary>
public interface IChatCurrentUser
{
    Task<ChatUser?> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>Pushes chat changes to connected browsers. The ASP.NET Core host implements this with SignalR.</summary>
public interface IChatRealtime
{
    Task PublishRoomMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default);

    Task PublishPrivateMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default);

    Task PublishMessageRemovedAsync(ChatMessageRemovedDto message, CancellationToken cancellationToken = default);

    Task PublishRoomAddedAsync(ChatRoomDto room, CancellationToken cancellationToken = default);

    Task PublishRoomUpdatedAsync(ChatRoomUpdatedDto room, CancellationToken cancellationToken = default);

    Task PublishRoomRemovedAsync(ChatRoomRemovedDto room, CancellationToken cancellationToken = default);

    /// <summary>Tells layout badges that <paramref name="userId"/>'s unread count changed.</summary>
    Task PublishUnreadChangedAsync(string userId, CancellationToken cancellationToken = default);
}
