namespace EasyChatz.Services;

public sealed class ChatNotificationService(
    IRepository<MessageNotification> notifications,
    IChatUserDirectory directory,
    IChatCurrentUser currentUser) : IChatNotificationService
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

        return Result.Success();
    }

    private sealed record UnreadRow(string FromUserId, int Count);
}
