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
public sealed record ChatRoomDto(
    int Id,
    string Name,
    string AdminUserId,
    string AdminDisplayName);

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
public sealed record ChatMessageDto(
    int Id,
    string Content,
    DateTime Timestamp,
    string FromUserId,
    string FromDisplayName,
    int? ToRoomId,
    string? ToRoomName,
    string? ToUserId);

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
public sealed record ChatConversationDto(string UserId, string DisplayName, int UnreadCount);

/// <summary>
/// An unread direct message that no host channel (email, push, anything else) has taken yet.
/// A background job reads these. They are not exposed to the browser.
/// </summary>
public sealed record ChatPendingNotice(
    int Id,
    int MessageId,
    string FromUserId,
    string ToUserId,
    string Content,
    DateTime CreatedAtUtc);

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
