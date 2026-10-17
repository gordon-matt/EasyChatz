namespace EasyChatz.Models;

/// <summary>
/// A person who can send and receive chat. The host resolves these from its own user store
/// through <see cref="Services.IChatUserDirectory"/>; EasyChatz does not keep accounts.
/// </summary>
public sealed record ChatUser(string Id, string DisplayName, bool IsAdministrator, bool CanCreateRooms);

public sealed record ChatRoomDto(
    int Id,
    string Name,
    string AdminUserId,
    string AdminDisplayName);

public sealed record ChatRoomUpdatedDto(
    int Id,
    string Name,
    string PreviousName,
    string AdminUserId,
    string AdminDisplayName);

public sealed record ChatRoomRemovedDto(int RoomId, string RoomName);

public sealed record ChatMessageDto(
    int Id,
    string Content,
    DateTime Timestamp,
    string FromUserId,
    string FromDisplayName,
    int? ToRoomId,
    string? ToRoomName,
    string? ToUserId);

public sealed record ChatMessageRemovedDto(int MessageId, int? RoomId, string? RoomName, string? ToUserId, string FromUserId);

public sealed record ChatConversationDto(string UserId, string DisplayName, int UnreadCount);

public sealed record ChatPresenceDto(string ConnectionId, string UserId, string DisplayName, bool IsAdministrator);

public sealed record SaveRoomRequest(string? Name);

public sealed record SendMessageRequest(string? Content);

/// <summary>Raised on the page as <c>ec-conversation</c> and through <c>OnConversationChanged</c>.</summary>
public sealed record ChatConversationEvent(string Kind, string Id, string Name);
