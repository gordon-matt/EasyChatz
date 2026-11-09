using EasyChatz.Models;

namespace EasyChatz.Services;

/// <summary>
/// Defines the contract for the chat service.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Gets the current user.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat user.</returns>
    Task<Result<ChatUser>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>The host's switches for archive, block, and delete. Safe to call before any of those actions.</summary>
    Task<Result<ChatFeaturesDto>> GetFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the rooms.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat rooms.</returns>
    Task<Result<IReadOnlyList<ChatRoomDto>>> GetRoomsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the room.
    /// </summary>
    /// <param name="roomId">The room identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat room.</returns>
    Task<Result<ChatRoomDto>> GetRoomAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the room.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat room.</returns>
    Task<Result<ChatRoomDto>> CreateRoomAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the room.
    /// </summary>
    /// <param name="roomId">The room identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat room updated.</returns>
    Task<Result<ChatRoomUpdatedDto>> UpdateRoomAsync(int roomId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the room.
    /// </summary>
    /// <param name="roomId">The room identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat room removed.</returns>
    Task<Result<ChatRoomRemovedDto>> DeleteRoomAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>Hides the room and stops new messages. The owner or an administrator can do this, and can undo it.</summary>
    Task<Result<ChatRoomDto>> ArchiveRoomAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>Puts an archived room back. Same people who can archive it.</summary>
    Task<Result<ChatRoomDto>> UnarchiveRoomAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the room messages.
    /// </summary>
    /// <param name="roomId">The room identifier.</param>
    /// <param name="take">The take.</param>
    /// <param name="beforeId">The before identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat messages.</returns>
    Task<Result<IReadOnlyList<ChatMessageDto>>> GetRoomMessagesAsync(int roomId, int? take = null, int? beforeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the room message.
    /// </summary>
    /// <param name="roomId">The room identifier.</param>
    /// <param name="content">The content.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat message.</returns>
    Task<Result<ChatMessageDto>> SendRoomMessageAsync(int roomId, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the conversations.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat conversations.</returns>
    Task<Result<IReadOnlyList<ChatConversationDto>>> GetConversationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the conversation.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat conversation.</returns>
    Task<Result<ChatConversationDto>> GetConversationAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the private messages.
    /// </summary>
    /// <param name="otherUserId">The other user identifier.</param>
    /// <param name="take">The take.</param>
    /// <param name="beforeId">The before identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat messages.</returns>
    Task<Result<IReadOnlyList<ChatMessageDto>>> GetPrivateMessagesAsync(string otherUserId, int? take = null, int? beforeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the private message.
    /// </summary>
    /// <param name="toUserId">The to user identifier.</param>
    /// <param name="content">The content.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat message.</returns>
    Task<Result<ChatMessageDto>> SendPrivateMessageAsync(string toUserId, string content, CancellationToken cancellationToken = default);

    /// <summary>Stops <paramref name="userId"/> from sending direct messages to the signed-in user.</summary>
    Task<Result<ChatConversationDto>> BlockUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Lets <paramref name="userId"/> send direct messages again.</summary>
    Task<Result<ChatConversationDto>> UnblockUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the author's own message and keeps a note of who deleted it and when.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat message.</returns>
    Task<Result<ChatMessageDto>> DeleteMessageAsync(int messageId, CancellationToken cancellationToken = default);

    /// <summary>People matching <paramref name="term"/> who can be mentioned in a room. The signed-in user is left out.</summary>
    Task<Result<IReadOnlyList<ChatMentionDto>>> SearchMentionsAsync(string? term, int take = 8, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for the chat notification service.
/// </summary>
public interface IChatNotificationService
{
    /// <summary>
    /// Creates the value.
    /// </summary>
    /// <param name="chatMessageId">The chat message identifier.</param>
    /// <param name="fromUserId">The from user identifier.</param>
    /// <param name="toUserId">The to user identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateAsync(int chatMessageId, string fromUserId, string toUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the unread.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat conversations.</returns>
    Task<Result<IReadOnlyList<ChatConversationDto>>> GetUnreadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the mark read.
    /// </summary>
    /// <param name="fromUserId">The from user identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    Task<Result> MarkReadAsync(string fromUserId, CancellationToken cancellationToken = default);

    /// <summary>Unread direct messages plus room mentions, for a layout badge.</summary>
    Task<Result<int>> CountUnreadAsync(CancellationToken cancellationToken = default)
    {
        return CountDirectAsync(cancellationToken);

        async Task<Result<int>> CountDirectAsync(CancellationToken token)
        {
            var unread = await GetUnreadAsync(token);
            return unread.IsSuccess
                ? Result<int>.Success(unread.Value?.Sum(item => item.UnreadCount) ?? 0)
                : unread.To<int>();
        }
    }

    /// <summary>Clears mention notices once the user opens that room. Direct messages are left alone.</summary>
    Task<Result> MarkRoomReadAsync(int roomId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());

    /// <summary>Unread mention counts keyed by room, so a room list can show where they happened.</summary>
    Task<Result<IReadOnlyDictionary<int, int>>> GetUnreadRoomMentionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<IReadOnlyDictionary<int, int>>.Success(new Dictionary<int, int>()));
}

/// <summary>
/// Unread direct messages waiting for a host-owned channel. Call this from a trusted background job,
/// not from a page: it can see every recipient.
/// </summary>
public interface IChatDeliveryQueue
{
    /// <summary>
    /// Lists the pending.
    /// </summary>
    /// <param name="createdBeforeUtc">The created before utc.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the chat pending notices.</returns>
    Task<IReadOnlyList<ChatPendingNotice>> ListPendingAsync(DateTime createdBeforeUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the mark delivered operation.
    /// </summary>
    /// <param name="noticeIds">The notice ids.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MarkDeliveredAsync(IReadOnlyCollection<int> noticeIds, CancellationToken cancellationToken = default);
}

/// <summary>The host's user store. EasyChatz never authenticates; it only asks who someone is.</summary>
public interface IChatUserDirectory
{
    /// <summary>
    /// Finds the value.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the chat user.</returns>
    Task<ChatUser?> FindAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the many.
    /// </summary>
    /// <param name="userIds">The user ids.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the string, chat user i read only dictionary.</returns>
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
    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the chat user.</returns>
    Task<ChatUser?> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>Pushes chat changes to connected browsers. The ASP.NET Core host implements this with SignalR.</summary>
public interface IChatRealtime
{
    /// <summary>
    /// Publishes the room message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishRoomMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the private message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishPrivateMessageAsync(ChatMessageDto message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the message removed.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishMessageRemovedAsync(ChatMessageRemovedDto message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the room added.
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishRoomAddedAsync(ChatRoomDto room, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the room updated.
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishRoomUpdatedAsync(ChatRoomUpdatedDto room, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the room removed.
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishRoomRemovedAsync(ChatRoomRemovedDto room, CancellationToken cancellationToken = default);

    /// <summary>Tells layout badges that <paramref name="userId"/>'s unread count changed.</summary>
    Task PublishUnreadChangedAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delivers a room message to people who were mentioned, including when they do not have that room open.
    /// </summary>
    Task PublishMentionAsync(ChatMessageDto message, IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <summary>Replaces a message in open threads with the deleted note, instead of removing the row.</summary>
    Task PublishMessageDeletedAsync(ChatMessageDto message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <summary>Tells every client that a room was archived or restored.</summary>
    Task PublishRoomArchivedAsync(ChatRoomDto room, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <summary>Tells both people that a direct-message block was added or removed.</summary>
    Task PublishUserBlockChangedAsync(ChatBlockChangedDto change, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
