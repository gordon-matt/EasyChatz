namespace EasyChatz.Services;

public sealed class ChatNotificationService(
    IRepository<MessageNotification> notifications,
    IRepository<ChatMessage> messages,
    IChatUserDirectory directory,
    IChatCurrentUser currentUser,
    IChatRealtime realtime) : IChatNotificationService, IChatDeliveryQueue
{
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

    public async Task<Result<IReadOnlyList<ChatConversationDto>>> GetUnreadAsync(CancellationToken cancellationToken = default)
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
        {
            return Result<IReadOnlyList<ChatConversationDto>>.Unauthorized("Please sign in to continue.");
        }

        List<UnreadRow> rows;
        using (var connection = notifications.OpenConnection())
        {
            rows = await connection.Query(n => n.ToUserId == actor.Id && !n.IsRead)
                .GroupBy(n => n.FromUserId)
                .Select(g => new UnreadRow(g.Key, g.Count()))
                .ToListAsync(cancellationToken);
        }

        var people = await directory.FindManyAsync(rows.Select(r => r.FromUserId), cancellationToken);

        return Result<IReadOnlyList<ChatConversationDto>>.Success(
            rows.Select(r => new ChatConversationDto(
                r.FromUserId,
                people.TryGetValue(r.FromUserId, out var person) ? person.DisplayName : r.FromUserId,
                r.Count)).ToList());
    }

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

        await notifications.UpdateAsync(
            n => n.ToUserId == actor.Id && n.FromUserId == fromUserId && !n.IsRead,
            setters => setters.SetProperty(n => n.IsRead, true),
            ContextOptions.ForCancellationToken(cancellationToken));

        await realtime.PublishUnreadChangedAsync(actor.Id, cancellationToken);
        return Result.Success();
    }

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
