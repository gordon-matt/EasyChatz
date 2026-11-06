namespace EasyChatz.Services;

/// <summary>
/// Represents the chat notification service.
/// </summary>
public sealed class ChatNotificationService(
    IRepository<MessageNotification> notifications,
    IRepository<ChatMessage> messages,
    IChatUserDirectory directory,
    IChatCurrentUser currentUser,
    IChatRealtime realtime) : IChatNotificationService, IChatDeliveryQueue
{
    /// <summary>
    /// Creates the value.
    /// </summary>
    /// <param name="chatMessageId">The chat message identifier.</param>
    /// <param name="fromUserId">The from user identifier.</param>
    /// <param name="toUserId">The to user identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CreateAsync(int chatMessageId, string fromUserId, string toUserId, CancellationToken cancellationToken = default)
    {
        await notifications.InsertAsync(new MessageNotification
        {
            ChatMessageId = chatMessageId,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow,
        }, ContextOptions.ForCancellationToken(cancellationToken));
    }

    /// <summary>
    /// Gets the unread.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the chat conversations.</returns>
    public async Task<Result<IReadOnlyList<ChatConversationDto>>> GetUnreadAsync(CancellationToken cancellationToken = default)
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
        {
            return Result<IReadOnlyList<ChatConversationDto>>.Unauthorized("Please sign in to continue.");
        }

        var unread = await UnreadAsync(actor.Id, cancellationToken);
        var roomMessages = await RoomMessageIdsAsync(unread.Select(n => n.ChatMessageId), roomId: null, cancellationToken);
        var rows = unread
            .Where(n => !roomMessages.Contains(n.ChatMessageId))
            .GroupBy(n => n.FromUserId, StringComparer.Ordinal)
            .Select(g => new UnreadRow(g.Key, g.Count()))
            .ToList();

        var people = await directory.FindManyAsync(rows.Select(r => r.FromUserId), cancellationToken);

        return Result<IReadOnlyList<ChatConversationDto>>.Success(
            rows.Select(r => new ChatConversationDto(
                r.FromUserId,
                people.TryGetValue(r.FromUserId, out var person) ? person.DisplayName : r.FromUserId,
                r.Count)).ToList());
    }

    /// <summary>
    /// Gets the mark read.
    /// </summary>
    /// <param name="fromUserId">The from user identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<Result> MarkReadAsync(string fromUserId, CancellationToken cancellationToken = default)
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
        {
            return Result.Unauthorized("Please sign in to continue.");
        }

        if (string.IsNullOrWhiteSpace(fromUserId))
        {
            return Result.Invalid(new ValidationError(nameof(fromUserId), "A sender is required."));
        }

        var unread = await UnreadAsync(actor.Id, cancellationToken);
        var roomMessages = await RoomMessageIdsAsync(unread.Select(n => n.ChatMessageId), roomId: null, cancellationToken);
        int[] directIds = unread
            .Where(n => string.Equals(n.FromUserId, fromUserId, StringComparison.Ordinal) && !roomMessages.Contains(n.ChatMessageId))
            .Select(n => n.Id)
            .ToArray();
        if (directIds.Length == 0)
        {
            return Result.Success();
        }

        await notifications.UpdateAsync(
            n => directIds.Contains(n.Id) && !n.IsRead,
            setters => setters.SetProperty(n => n.IsRead, true),
            ContextOptions.ForCancellationToken(cancellationToken));

        await realtime.PublishUnreadChangedAsync(actor.Id, cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Counts the unread.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the int.</returns>
    public async Task<Result<int>> CountUnreadAsync(CancellationToken cancellationToken = default)
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
        {
            return Result<int>.Unauthorized("Please sign in to continue.");
        }

        var unread = await UnreadAsync(actor.Id, cancellationToken);
        return Result<int>.Success(unread.Count);
    }

    /// <summary>
    /// Gets the unread room mentions.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the int, int i read only dictionary.</returns>
    public async Task<Result<IReadOnlyDictionary<int, int>>> GetUnreadRoomMentionsAsync(CancellationToken cancellationToken = default)
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
        {
            return Result<IReadOnlyDictionary<int, int>>.Unauthorized("Please sign in to continue.");
        }

        var unread = await UnreadAsync(actor.Id, cancellationToken);
        int[] messageIds = unread.Select(n => n.ChatMessageId).Distinct().ToArray();
        if (messageIds.Length == 0)
        {
            return Result<IReadOnlyDictionary<int, int>>.Success(new Dictionary<int, int>());
        }

        Dictionary<int, int> roomByMessage;
        using (var connection = messages.OpenConnection())
        {
            var found = await connection.Query(m => messageIds.Contains(m.Id) && m.ToRoomId != null)
                .Select(m => new { m.Id, m.ToRoomId })
                .ToListAsync(cancellationToken);
            roomByMessage = found.ToDictionary(m => m.Id, m => m.ToRoomId!.Value);
        }

        IReadOnlyDictionary<int, int> counts = unread
            .Where(n => roomByMessage.ContainsKey(n.ChatMessageId))
            .GroupBy(n => roomByMessage[n.ChatMessageId])
            .ToDictionary(g => g.Key, g => g.Count());

        return Result<IReadOnlyDictionary<int, int>>.Success(counts);
    }

    /// <summary>
    /// Gets the mark room read.
    /// </summary>
    /// <param name="roomId">The room identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<Result> MarkRoomReadAsync(int roomId, CancellationToken cancellationToken = default)
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
        {
            return Result.Unauthorized("Please sign in to continue.");
        }

        var unread = await UnreadAsync(actor.Id, cancellationToken);
        var inRoom = await RoomMessageIdsAsync(unread.Select(n => n.ChatMessageId), roomId, cancellationToken);
        int[] noticeIds = unread
            .Where(n => inRoom.Contains(n.ChatMessageId))
            .Select(n => n.Id)
            .ToArray();
        if (noticeIds.Length == 0)
        {
            return Result.Success();
        }

        await notifications.UpdateAsync(
            n => noticeIds.Contains(n.Id) && !n.IsRead,
            setters => setters.SetProperty(n => n.IsRead, true),
            ContextOptions.ForCancellationToken(cancellationToken));

        await realtime.PublishUnreadChangedAsync(actor.Id, cancellationToken);
        return Result.Success();
    }

    private async Task<List<MessageNotification>> UnreadAsync(string userId, CancellationToken cancellationToken)
    {
        using var connection = notifications.OpenConnection();
        return await connection.Query(n => n.ToUserId == userId && !n.IsRead).ToListAsync(cancellationToken);
    }

    /// <summary>Message ids among <paramref name="messageIds"/> that belong to a room, or to one room when <paramref name="roomId"/> is set.</summary>
    private async Task<HashSet<int>> RoomMessageIdsAsync(IEnumerable<int> messageIds, int? roomId, CancellationToken cancellationToken)
    {
        int[] ids = messageIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        using var connection = messages.OpenConnection();
        List<int> found = roomId is int only
            ? await connection.Query(m => ids.Contains(m.Id) && m.ToRoomId == only).Select(m => m.Id).ToListAsync(cancellationToken)
            : await connection.Query(m => ids.Contains(m.Id) && m.ToRoomId != null).Select(m => m.Id).ToListAsync(cancellationToken);
        return found.ToHashSet();
    }

    /// <summary>
    /// Lists the pending.
    /// </summary>
    /// <param name="createdBeforeUtc">The created before utc.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the chat pending notices.</returns>
    public async Task<IReadOnlyList<ChatPendingNotice>> ListPendingAsync(DateTime createdBeforeUtc, CancellationToken cancellationToken = default)
    {
        List<MessageNotification> rows;
        using (var connection = notifications.OpenConnection())
        {
            rows = await connection.Query(n => !n.IsRead && n.DeliveredAtUtc == null && n.CreatedAtUtc <= createdBeforeUtc)
                .OrderBy(n => n.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        if (rows.Count == 0)
        {
            return [];
        }

        int[] ids = rows.Select(n => n.ChatMessageId).Distinct().ToArray();
        Dictionary<int, string> content;
        using (var connection = messages.OpenConnection())
        {
            var found = await connection.Query(m => ids.Contains(m.Id)).ToListAsync(cancellationToken);
            content = found.ToDictionary(m => m.Id, m => m.Content);
        }

        return rows
            .Where(n => content.ContainsKey(n.ChatMessageId))
            .Select(n => new ChatPendingNotice(n.Id, n.ChatMessageId, n.FromUserId, n.ToUserId, content[n.ChatMessageId], n.CreatedAtUtc))
            .ToList();
    }

    /// <summary>
    /// Performs the mark delivered operation.
    /// </summary>
    /// <param name="noticeIds">The notice ids.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task MarkDeliveredAsync(IReadOnlyCollection<int> noticeIds, CancellationToken cancellationToken = default)
    {
        int[] ids = noticeIds.Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        await notifications.UpdateAsync(
            n => ids.Contains(n.Id) && n.DeliveredAtUtc == null,
            setters => setters.SetProperty(n => n.DeliveredAtUtc, now),
            ContextOptions.ForCancellationToken(cancellationToken));
    }

    private sealed record UnreadRow(string FromUserId, int Count);
}
