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
}

public interface IChatNotificationService
{
    Task CreateAsync(int chatMessageId, string fromUserId, string toUserId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ChatConversationDto>>> GetUnreadAsync(CancellationToken cancellationToken = default);

    Task<Result> MarkReadAsync(string fromUserId, CancellationToken cancellationToken = default);
}

/// <summary>The host's user store. EasyChatz never authenticates; it only asks who someone is.</summary>
public interface IChatUserDirectory
{
    Task<ChatUser?> FindAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, ChatUser>> FindManyAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
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
}
