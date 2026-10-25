using System.Text.RegularExpressions;

namespace EasyChatz.Services;

public sealed partial class ChatService(
    ILogger<ChatService> logger,
    IOptions<EasyChatzOptions> options,
    IRepository<ChatRoom> rooms,
    IRepository<ChatMessage> messages,
    IChatNotificationService notifications,
    IChatUserDirectory directory,
    IChatCurrentUser currentUser,
    IChatRealtime realtime) : IChatService
{
    public async Task<Result<ChatUser>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        return actor is null
            ? Result<ChatUser>.Unauthorized("Please sign in to continue.")
            : Result<ChatUser>.Success(actor);
    }

    public async Task<Result<IReadOnlyList<ChatRoomDto>>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        string announcements = options.Value.AnnouncementsRoomName;
        var found = (await rooms.FindAsync(new SearchOptions<ChatRoom>
        {
            OrderBy = query => query.OrderByDescending(r => r.Name == announcements).ThenBy(r => r.Name),
            CancellationToken = cancellationToken,
        })).ToList();

        var names = await NamesAsync(found.Select(r => r.AdminUserId), cancellationToken);
        return Result<IReadOnlyList<ChatRoomDto>>.Success(found.Select(r => ToRoom(r, names)).ToList());
    }

    public async Task<Result<ChatRoomDto>> GetRoomAsync(int roomId, CancellationToken cancellationToken = default)
    {
        var room = await rooms.FindOneAsync(roomId);
        if (room is null)
        {
            return Result<ChatRoomDto>.NotFound("Room not found.");
        }

        var names = await NamesAsync([room.AdminUserId], cancellationToken);
        return Result<ChatRoomDto>.Success(ToRoom(room, names));
    }

    public async Task<Result<ChatRoomDto>> CreateRoomAsync(string name, CancellationToken cancellationToken = default)
    {
        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<ChatRoomDto>();
        }

        if (!actor.Value.CanCreateRooms)
        {
            return Result<ChatRoomDto>.Forbidden("You can't create rooms.");
        }

        var validated = await ValidateRoomNameAsync(name, existingId: null, cancellationToken);
        if (!validated.IsSuccess)
        {
            return validated.To<ChatRoomDto>();
        }

        var saved = await rooms.InsertAsync(new ChatRoom
        {
            Name = validated.Value,
            AdminUserId = actor.Value.Id,
            CreatedAtUtc = DateTime.UtcNow,
        }, ContextOptions.ForCancellationToken(cancellationToken));

        logger.LogInformation("User {UserId} created room {RoomId} '{Name}'", actor.Value.Id, saved.Id, saved.Name);

        var dto = new ChatRoomDto(saved.Id, saved.Name, saved.AdminUserId, actor.Value.DisplayName);
        await realtime.PublishRoomAddedAsync(dto, cancellationToken);
        return Result<ChatRoomDto>.Success(dto);
    }

    public async Task<Result<ChatRoomUpdatedDto>> UpdateRoomAsync(int roomId, string name, CancellationToken cancellationToken = default)
    {
        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<ChatRoomUpdatedDto>();
        }

        var room = await rooms.FindOneAsync(roomId);
        if (room is null)
        {
            return Result<ChatRoomUpdatedDto>.NotFound("Room not found.");
        }

        if (IsAnnouncements(room.Name))
        {
            return Result<ChatRoomUpdatedDto>.Forbidden("Announcements can't be renamed.");
        }

        if (!string.Equals(room.AdminUserId, actor.Value.Id, StringComparison.Ordinal))
        {
            return Result<ChatRoomUpdatedDto>.Forbidden("Only the room owner can rename it.");
        }

        string previous = room.Name;
        var validated = await ValidateRoomNameAsync(name, room.Id, cancellationToken);
        if (!validated.IsSuccess)
        {
            return validated.To<ChatRoomUpdatedDto>();
        }

        room.Name = validated.Value;
        await rooms.UpdateAsync(room, ContextOptions.ForCancellationToken(cancellationToken));

        var dto = new ChatRoomUpdatedDto(room.Id, room.Name, previous, room.AdminUserId, actor.Value.DisplayName);
        await realtime.PublishRoomUpdatedAsync(dto, cancellationToken);
        return Result<ChatRoomUpdatedDto>.Success(dto);
    }

    public async Task<Result<ChatRoomRemovedDto>> DeleteRoomAsync(int roomId, CancellationToken cancellationToken = default)
    {
        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<ChatRoomRemovedDto>();
        }

        var room = await rooms.FindOneAsync(roomId);
        if (room is null)
        {
            return Result<ChatRoomRemovedDto>.NotFound("Room not found.");
        }

        if (IsAnnouncements(room.Name))
        {
            return Result<ChatRoomRemovedDto>.Forbidden("Announcements can't be deleted.");
        }

        if (!actor.Value.IsAdministrator && !string.Equals(room.AdminUserId, actor.Value.Id, StringComparison.Ordinal))
        {
            return Result<ChatRoomRemovedDto>.Forbidden("Only the room owner can delete it.");
        }

        var dto = new ChatRoomRemovedDto(room.Id, room.Name);
        await rooms.DeleteAsync(room, ContextOptions.ForCancellationToken(cancellationToken));
        logger.LogInformation("User {UserId} deleted room {RoomId}", actor.Value.Id, roomId);

        await realtime.PublishRoomRemovedAsync(dto, cancellationToken);
        return Result<ChatRoomRemovedDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> GetRoomMessagesAsync(
        int roomId, int? take = null, int? beforeId = null, CancellationToken cancellationToken = default)
    {
        var room = await rooms.FindOneAsync(roomId);
        if (room is null)
        {
            return Result<IReadOnlyList<ChatMessageDto>>.NotFound("Room not found.");
        }

        var page = await PageAsync(
            m => m.ToRoomId == roomId && (beforeId == null || m.Id < beforeId),
            take,
            cancellationToken);

        return Result<IReadOnlyList<ChatMessageDto>>.Success(await MapAsync(page, room.Name, cancellationToken));
    }

    public async Task<Result<ChatMessageDto>> SendRoomMessageAsync(int roomId, string content, CancellationToken cancellationToken = default)
    {
        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<ChatMessageDto>();
        }

        var room = await rooms.FindOneAsync(roomId);
        if (room is null)
        {
            return Result<ChatMessageDto>.NotFound("Room not found.");
        }

        if (IsAnnouncements(room.Name) && !actor.Value.IsAdministrator)
        {
            return Result<ChatMessageDto>.Forbidden("Only an administrator can post in Announcements.");
        }

        var clean = await CleanMessageAsync(content, allowMentions: true, cancellationToken);
        if (!clean.IsSuccess)
        {
            return clean.To<ChatMessageDto>();
        }

        var saved = await messages.InsertAsync(new ChatMessage
        {
            Content = clean.Value,
            Timestamp = DateTime.UtcNow,
            FromUserId = actor.Value.Id,
            ToRoomId = room.Id,
        }, ContextOptions.ForCancellationToken(cancellationToken));

        var dto = ToMessage(saved, actor.Value.DisplayName, room.Name);
        await realtime.PublishRoomMessageAsync(dto, cancellationToken);
        return Result<ChatMessageDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<ChatConversationDto>>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<IReadOnlyList<ChatConversationDto>>();
        }

        var latest = await PartnerActivityAsync(actor.Value.Id, cancellationToken);
        var unread = await UnreadCountsAsync(cancellationToken);
        var people = await directory.FindManyAsync(latest.Select(p => p.UserId), cancellationToken);

        return Result<IReadOnlyList<ChatConversationDto>>.Success(latest.Select(p => new ChatConversationDto(
            p.UserId,
            people.TryGetValue(p.UserId, out var person) ? person.DisplayName : p.UserId,
            unread.GetValueOrDefault(p.UserId))).ToList());
    }

    public async Task<Result<ChatConversationDto>> GetConversationAsync(string userId, CancellationToken cancellationToken = default)
    {
        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<ChatConversationDto>();
        }

        var person = await directory.FindAsync(userId, cancellationToken);
        if (person is null)
        {
            return Result<ChatConversationDto>.NotFound("That person could not be found.");
        }

        var unread = await UnreadCountsAsync(cancellationToken);
        return Result<ChatConversationDto>.Success(
            new ChatConversationDto(person.Id, person.DisplayName, unread.GetValueOrDefault(person.Id)));
    }

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> GetPrivateMessagesAsync(
        string otherUserId, int? take = null, int? beforeId = null, CancellationToken cancellationToken = default)
    {
        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<IReadOnlyList<ChatMessageDto>>();
        }

        string me = actor.Value.Id;
        var page = await PageAsync(
            m => m.ToUserId != null &&
                 ((m.FromUserId == me && m.ToUserId == otherUserId) || (m.FromUserId == otherUserId && m.ToUserId == me)) &&
                 (beforeId == null || m.Id < beforeId),
            take,
            cancellationToken);

        return Result<IReadOnlyList<ChatMessageDto>>.Success(await MapAsync(page, roomName: null, cancellationToken));
    }

    public async Task<Result<ChatMessageDto>> SendPrivateMessageAsync(string toUserId, string content, CancellationToken cancellationToken = default)
    {
        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<ChatMessageDto>();
        }

        if (string.IsNullOrWhiteSpace(toUserId))
        {
            return Result<ChatMessageDto>.Invalid(new ValidationError(nameof(toUserId), "Choose someone to message."));
        }

        if (string.Equals(toUserId, actor.Value.Id, StringComparison.Ordinal))
        {
            return Result<ChatMessageDto>.Invalid(new ValidationError(nameof(toUserId), "You can't message yourself."));
        }

        var recipient = await directory.FindAsync(toUserId, cancellationToken);
        if (recipient is null)
        {
            return Result<ChatMessageDto>.NotFound("That person could not be found.");
        }

        var clean = await CleanMessageAsync(content, allowMentions: false, cancellationToken);
        if (!clean.IsSuccess)
        {
            return clean.To<ChatMessageDto>();
        }

        var saved = await messages.InsertAsync(new ChatMessage
        {
            Content = clean.Value,
            Timestamp = DateTime.UtcNow,
            FromUserId = actor.Value.Id,
            ToUserId = recipient.Id,
        }, ContextOptions.ForCancellationToken(cancellationToken));

        await notifications.CreateAsync(saved.Id, actor.Value.Id, recipient.Id, cancellationToken);

        var dto = ToMessage(saved, actor.Value.DisplayName, roomName: null);
        await realtime.PublishPrivateMessageAsync(dto, cancellationToken);
        return Result<ChatMessageDto>.Success(dto);
    }

    public async Task<Result<ChatMessageRemovedDto>> DeleteMessageAsync(int messageId, CancellationToken cancellationToken = default)
    {
        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<ChatMessageRemovedDto>();
        }

        var message = await messages.FindOneAsync(messageId);
        if (message is null)
        {
            return Result<ChatMessageRemovedDto>.NotFound("Message not found.");
        }

        if (!string.Equals(message.FromUserId, actor.Value.Id, StringComparison.Ordinal))
        {
            return Result<ChatMessageRemovedDto>.Forbidden("You can only delete your own messages.");
        }

        string? roomName = null;
        if (message.ToRoomId is int roomId)
        {
            roomName = (await rooms.FindOneAsync(roomId))?.Name;
        }

        var dto = new ChatMessageRemovedDto(message.Id, message.ToRoomId, roomName, message.ToUserId, message.FromUserId);
        await messages.DeleteAsync(message, ContextOptions.ForCancellationToken(cancellationToken));
        await realtime.PublishMessageRemovedAsync(dto, cancellationToken);
        return Result<ChatMessageRemovedDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<ChatMentionDto>>> SearchMentionsAsync(string? term, int take = 8, CancellationToken cancellationToken = default)
    {
        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<IReadOnlyList<ChatMentionDto>>();
        }

        string query = term?.Trim() ?? string.Empty;
        int count = Math.Clamp(take, 1, 20);

        // One extra, so dropping the signed-in user still leaves a full list.
        var found = await directory.SearchAsync(query, count + 1, cancellationToken);
        var people = found
            .Where(p => !string.Equals(p.Id, actor.Value.Id, StringComparison.Ordinal))
            .Take(count)
            .Select(p => new ChatMentionDto(p.Id, p.DisplayName))
            .ToList();

        return Result<IReadOnlyList<ChatMentionDto>>.Success(people);
    }

    private async Task<Result<ChatUser>> RequireAsync(CancellationToken cancellationToken)
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        return actor is null
            ? Result<ChatUser>.Unauthorized("Please sign in to continue.")
            : Result<ChatUser>.Success(actor);
    }

    private async Task<Result<string>> ValidateRoomNameAsync(string? name, int? existingId, CancellationToken cancellationToken)
    {
        string trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return Result<string>.Invalid(new ValidationError(nameof(name), "Give the room a name."));
        }

        int max = Math.Clamp(options.Value.MaxRoomNameLength, 1, Constants.MaxLengths.RoomName);
        if (trimmed.Length > max)
        {
            return Result<string>.Invalid(new ValidationError(nameof(name), $"Room names can be at most {max} characters."));
        }

        if (IsAnnouncements(trimmed))
        {
            return Result<string>.Forbidden($"'{options.Value.AnnouncementsRoomName}' is reserved.");
        }

        string lowered = trimmed.ToLower();
        bool taken = await rooms.ExistsAsync(
            r => r.Name.ToLower() == lowered && (existingId == null || r.Id != existingId),
            ContextOptions.ForCancellationToken(cancellationToken));

        return taken
            ? Result<string>.Conflict($"There is already a room called '{trimmed}'.")
            : Result<string>.Success(trimmed);
    }

    /// <summary>
    /// Strips markup, then settles mentions: in a room each token is checked against the directory
    /// and rewritten with the person's real name, so a hand-built token cannot pose as someone else.
    /// Anywhere else tokens are flattened to plain text.
    /// </summary>
    private async Task<Result<string>> CleanMessageAsync(string? content, bool allowMentions, CancellationToken cancellationToken)
    {
        string clean = string.IsNullOrWhiteSpace(content) ? string.Empty : StripHtmlRegex().Replace(content, string.Empty).Trim();
        if (clean.Length == 0)
        {
            return Result<string>.Invalid(new ValidationError(nameof(content), "Message content is required."));
        }

        var mentioned = allowMentions ? ChatMentions.UserIds(clean) : [];
        if (mentioned.Count > 0)
        {
            var known = await directory.FindManyAsync(mentioned, cancellationToken);
            clean = ChatMentions.Normalize(clean, known);
        }
        else
        {
            clean = ChatMentions.ToPlainText(clean);
        }

        int max = Math.Clamp(options.Value.MaxMessageLength, 1, Constants.MaxLengths.Message);
        if (clean.Length > max)
        {
            return Result<string>.Invalid(new ValidationError(nameof(content), $"Messages can be at most {max} characters."));
        }

        return Result<string>.Success(clean);
    }

    private bool IsAnnouncements(string name) =>
        string.Equals(name, options.Value.AnnouncementsRoomName, StringComparison.OrdinalIgnoreCase);

    private int PageSize(int? take) =>
        Math.Clamp(take ?? options.Value.DefaultPageSize, 1, Math.Max(1, options.Value.MaxPageSize));

    private async Task<List<ChatMessage>> PageAsync(
        System.Linq.Expressions.Expression<Func<ChatMessage, bool>> query,
        int? take,
        CancellationToken cancellationToken)
    {
        var found = await messages.FindAsync(new SearchOptions<ChatMessage>
        {
            Query = query,
            OrderBy = q => q.OrderByDescending(m => m.Id),
            PageNumber = 1,
            PageSize = PageSize(take),
            CancellationToken = cancellationToken,
        });

        return found.OrderBy(m => m.Id).ToList();
    }

    private async Task<IReadOnlyList<ChatMessageDto>> MapAsync(IReadOnlyList<ChatMessage> page, string? roomName, CancellationToken cancellationToken)
    {
        var names = await NamesAsync(page.Select(m => m.FromUserId), cancellationToken);
        return page.Select(m => ToMessage(m, NameOf(names, m.FromUserId), roomName)).ToList();
    }

    private async Task<IReadOnlyDictionary<string, string>> NamesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken)
    {
        var people = await directory.FindManyAsync(userIds, cancellationToken);
        return people.ToDictionary(p => p.Key, p => p.Value.DisplayName, StringComparer.Ordinal);
    }

    private static string NameOf(IReadOnlyDictionary<string, string> names, string userId)
    {
        if (string.Equals(userId, Constants.SystemUserId, StringComparison.Ordinal))
        {
            return "EasyChatz";
        }

        return names.TryGetValue(userId, out string? name) && !string.IsNullOrWhiteSpace(name) ? name : userId;
    }

    private static ChatRoomDto ToRoom(ChatRoom room, IReadOnlyDictionary<string, string> names) =>
        new(room.Id, room.Name, room.AdminUserId, NameOf(names, room.AdminUserId));

    private static ChatMessageDto ToMessage(ChatMessage message, string fromDisplayName, string? roomName) =>
        new(message.Id, message.Content, message.Timestamp, message.FromUserId, fromDisplayName, message.ToRoomId, roomName, message.ToUserId);

    private async Task<List<PartnerActivity>> PartnerActivityAsync(string userId, CancellationToken cancellationToken)
    {
        List<PartnerRow> rows;
        using (var connection = messages.OpenConnection())
        {
            rows = await connection.Query(m => m.ToUserId != null && (m.FromUserId == userId || m.ToUserId == userId))
                .Select(m => new PartnerRow(m.FromUserId, m.ToUserId!, m.Timestamp))
                .ToListAsync(cancellationToken);
        }

        return rows
            .Select(r => new PartnerActivity(r.FromUserId == userId ? r.ToUserId : r.FromUserId, r.Timestamp))
            .GroupBy(r => r.UserId, StringComparer.Ordinal)
            .Select(g => new PartnerActivity(g.Key, g.Max(x => x.Timestamp)))
            .OrderByDescending(r => r.Timestamp)
            .ToList();
    }

    private async Task<Dictionary<string, int>> UnreadCountsAsync(CancellationToken cancellationToken)
    {
        var unread = await notifications.GetUnreadAsync(cancellationToken);
        if (!unread.IsSuccess || unread.Value is null)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        return unread.Value.ToDictionary(c => c.UserId, c => c.UnreadCount, StringComparer.Ordinal);
    }

    [GeneratedRegex(@"<.*?>")]
    private static partial Regex StripHtmlRegex();

    private sealed record PartnerRow(string FromUserId, string ToUserId, DateTime Timestamp);

    private sealed record PartnerActivity(string UserId, DateTime Timestamp);
}
