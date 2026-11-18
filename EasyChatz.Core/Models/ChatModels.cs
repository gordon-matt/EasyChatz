namespace EasyChatz.Models;

/// <summary>
/// A person who can send and receive chat. The host resolves these from its own user store
/// through <see cref="Services.IChatUserDirectory"/>; EasyChatz does not keep accounts.
/// </summary>
public sealed record ChatUser(string Id, string DisplayName, bool IsAdministrator, bool CanCreateRooms);

/// <summary>
/// Represents the chat room DTO.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="AdminUserId">The admin user identifier.</param>
/// <param name="AdminDisplayName">The admin display name.</param>
/// <param name="IsArchived">Whether the room is archived.</param>
public sealed record ChatRoomDto(
    int Id,
    string Name,
    string AdminUserId,
    string AdminDisplayName,
    bool IsArchived = false);

/// <summary>
/// Represents the chat room updated DTO.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="PreviousName">The previous name.</param>
/// <param name="AdminUserId">The admin user identifier.</param>
/// <param name="AdminDisplayName">The admin display name.</param>
public sealed record ChatRoomUpdatedDto(
    int Id,
    string Name,
    string PreviousName,
    string AdminUserId,
    string AdminDisplayName);

/// <summary>
/// Represents the chat room removed DTO.
/// </summary>
/// <param name="RoomId">The room identifier.</param>
/// <param name="RoomName">The room name.</param>
public sealed record ChatRoomRemovedDto(int RoomId, string RoomName);

/// <summary>
/// Represents the chat message DTO.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Content">The content.</param>
/// <param name="Timestamp">The timestamp.</param>
/// <param name="FromUserId">The from user identifier.</param>
/// <param name="FromDisplayName">The from display name.</param>
/// <param name="ToRoomId">The to room identifier.</param>
/// <param name="ToRoomName">The to room name.</param>
/// <param name="ToUserId">The to user identifier.</param>
/// <param name="IsDeleted">Whether the message was deleted.</param>
/// <param name="DeletedByUserId">The deleted by user identifier.</param>
/// <param name="DeletedByDisplayName">The deleted by display name.</param>
/// <param name="DeletedAtUtc">The deleted at utc.</param>
public sealed record ChatMessageDto(
    int Id,
    string Content,
    DateTime Timestamp,
    string FromUserId,
    string FromDisplayName,
    int? ToRoomId,
    string? ToRoomName,
    string? ToUserId,
    bool IsDeleted = false,
    string? DeletedByUserId = null,
    string? DeletedByDisplayName = null,
    DateTime? DeletedAtUtc = null);

/// <summary>
/// Represents the chat message removed DTO.
/// </summary>
/// <param name="MessageId">The message identifier.</param>
/// <param name="RoomId">The room identifier.</param>
/// <param name="RoomName">The room name.</param>
/// <param name="ToUserId">The to user identifier.</param>
/// <param name="FromUserId">The from user identifier.</param>
public sealed record ChatMessageRemovedDto(int MessageId, int? RoomId, string? RoomName, string? ToUserId, string FromUserId);

/// <summary>
/// Represents the chat conversation DTO.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="DisplayName">The display name.</param>
/// <param name="UnreadCount">The unread count.</param>
/// <param name="BlockedByMe">Whether the signed-in user blocked this person.</param>
/// <param name="HasBlockedMe">Whether this person blocked the signed-in user.</param>
/// <param name="Connection">Whether they can message each other yet. Accepted when connections are not required.</param>
/// <param name="Introduction">The note on a pending request. Empty once they are connected.</param>
public sealed record ChatConversationDto(
    string UserId,
    string DisplayName,
    int UnreadCount,
    bool BlockedByMe = false,
    bool HasBlockedMe = false,
    ChatConnectionStatus Connection = ChatConnectionStatus.Accepted,
    string? Introduction = null);

/// <summary>
/// Which optional actions the host has switched on. The service rejects an action that is off,
/// and the component hides it.
/// </summary>
/// <param name="AllowRoomArchive">Whether room archive is available.</param>
/// <param name="AllowUserBlocks">Whether direct-message blocks are available.</param>
/// <param name="AllowMessageDelete">Whether message delete is available.</param>
/// <param name="AllowRooms">Whether rooms are available. Direct messages stay available when this is false.</param>
/// <param name="RequireConnections">Whether a first direct message waits for the other person to accept.</param>
public sealed record ChatFeaturesDto(
    bool AllowRoomArchive,
    bool AllowUserBlocks,
    bool AllowMessageDelete,
    bool AllowRooms = true,
    bool RequireConnections = true);

/// <summary>Stored state of a connection request. Direction is separate.</summary>
public enum ChatConnectionState
{
    /// <summary>The requester is waiting for an answer.</summary>
    Pending = 0,

    /// <summary>Both people can message each other.</summary>
    Accepted = 1,

    /// <summary>The last request was declined.</summary>
    Declined = 2,
}

/// <summary>How the signed-in user stands with one other person.</summary>
public enum ChatConnectionStatus
{
    /// <summary>No request yet.</summary>
    None = 0,

    /// <summary>The signed-in user asked and is waiting.</summary>
    PendingOutgoing = 1,

    /// <summary>The other person asked and is waiting for an answer.</summary>
    PendingIncoming = 2,

    /// <summary>They can message each other.</summary>
    Accepted = 3,

    /// <summary>The last request was declined.</summary>
    Declined = 4,
}

/// <summary>A connection request changed. Both people receive it.</summary>
/// <param name="RequesterUserId">The person who asked.</param>
/// <param name="RecipientUserId">The person who was asked.</param>
/// <param name="Status">Pending, accepted, or declined. Direction is not included.</param>
/// <param name="Introduction">The note sent with the request. Empty when there was none.</param>
public sealed record ChatConnectionChangedDto(
    string RequesterUserId,
    string RecipientUserId,
    ChatConnectionState Status,
    string Introduction);

/// <summary>Asks someone to connect. <paramref name="Introduction"/> is optional.</summary>
/// <param name="Introduction">A short note. Empty asks without one.</param>
public sealed record ConnectionRequest(string? Introduction);

/// <summary>A direct-message block was added or removed. IsBlocked is false when it was removed.</summary>
/// <param name="BlockerUserId">The person who blocked.</param>
/// <param name="BlockedUserId">The person who was blocked.</param>
/// <param name="IsBlocked">Whether the block is in place.</param>
public sealed record ChatBlockChangedDto(string BlockerUserId, string BlockedUserId, bool IsBlocked);

/// <summary>
/// An unread direct message that no host channel (email, push, anything else) has taken yet.
/// A background job reads these across every tenant. They are not exposed to the browser.
/// </summary>
/// <param name="Id">The notice identifier.</param>
/// <param name="MessageId">The message identifier.</param>
/// <param name="FromUserId">The from user identifier.</param>
/// <param name="ToUserId">The to user identifier.</param>
/// <param name="Content">The content.</param>
/// <param name="CreatedAtUtc">When the notice was created, in UTC.</param>
/// <param name="TenantId">The tenant the message was stored in. Empty for a single-tenant host.</param>
public sealed record ChatPendingNotice(
    int Id,
    int MessageId,
    string FromUserId,
    string ToUserId,
    string Content,
    DateTime CreatedAtUtc,
    string TenantId = "");

/// <summary>A person offered in the mention suggestions while typing <c>@</c> in a room.</summary>
public sealed record ChatMentionDto(string UserId, string DisplayName);

/// <summary>
/// Represents the chat presence DTO.
/// </summary>
/// <param name="ConnectionId">The connection identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="DisplayName">The display name.</param>
/// <param name="IsAdministrator">The is administrator.</param>
public sealed record ChatPresenceDto(string ConnectionId, string UserId, string DisplayName, bool IsAdministrator);

/// <summary>
/// Represents the save room request.
/// </summary>
/// <param name="Name">The name.</param>
public sealed record SaveRoomRequest(string? Name);

/// <summary>
/// Represents the send message request.
/// </summary>
/// <param name="Content">The content.</param>
public sealed record SendMessageRequest(string? Content);

/// <summary>Raised on the page as <c>ec-conversation</c> and through <c>OnConversationChanged</c>.</summary>
public sealed record ChatConversationEvent(string Kind, string Id, string Name);
