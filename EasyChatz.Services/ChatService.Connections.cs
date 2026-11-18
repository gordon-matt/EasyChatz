namespace EasyChatz.Services;

public sealed partial class ChatService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ChatMentionDto>>> SearchPeopleAsync(string? term, int take = 8, CancellationToken cancellationToken = default)
    {
        if (!options.Value.RequireConnections)
        {
            return await SearchMentionsAsync(term, take, cancellationToken);
        }

        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<IReadOnlyList<ChatMentionDto>>();
        }

        int count = Math.Clamp(take, 1, 20);
        string query = term?.Trim() ?? string.Empty;
        var ids = await AcceptedIdsAsync(actor.Value.Id, cancellationToken);
        var people = await directory.FindManyAsync(ids, cancellationToken);
        var matches = people.Values
            .Where(person => query.Length == 0 || person.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(person => person.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .Select(person => new ChatMentionDto(person.Id, person.DisplayName))
            .ToList();
        return Result<IReadOnlyList<ChatMentionDto>>.Success(matches);
    }

    /// <inheritdoc />
    public async Task<Result<ChatConversationDto>> RequestConnectionAsync(string userId, string? introduction, CancellationToken cancellationToken = default)
    {
        if (!options.Value.RequireConnections)
        {
            return Result<ChatConversationDto>.Forbidden("Connection requests are turned off.");
        }

        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<ChatConversationDto>();
        }

        var other = await OtherPersonAsync(actor.Value.Id, userId, cancellationToken);
        if (!other.IsSuccess)
        {
            return other.To<ChatConversationDto>();
        }

        string note = string.Empty;
        if (!string.IsNullOrWhiteSpace(introduction))
        {
            var clean = await CleanMessageAsync(introduction, allowMentions: false, cancellationToken);
            if (!clean.IsSuccess)
            {
                return clean.To<ChatConversationDto>();
            }

            note = clean.Value;
        }

        var existing = await FindConnectionAsync(actor.Value.Id, other.Value.Id, cancellationToken);
        if (existing is { Status: ChatConnectionState.Accepted })
        {
            return Result<ChatConversationDto>.Success(await ConversationAsync(actor.Value.Id, other.Value, cancellationToken));
        }

        if (existing is { Status: ChatConnectionState.Pending } && existing.RecipientUserId == actor.Value.Id)
        {
            return Result<ChatConversationDto>.Success(await ConversationAsync(actor.Value.Id, other.Value, cancellationToken));
        }

        ChatConnection saved;
        if (existing is null)
        {
            saved = await connections.InsertAsync(new ChatConnection
            {
                RequesterUserId = actor.Value.Id,
                RecipientUserId = other.Value.Id,
                Status = ChatConnectionState.Pending,
                Introduction = note,
                CreatedAtUtc = DateTime.UtcNow,
            }, ContextOptions.ForCancellationToken(cancellationToken));
        }
        else
        {
            existing.RequesterUserId = actor.Value.Id;
            existing.RecipientUserId = other.Value.Id;
            existing.Status = ChatConnectionState.Pending;
            existing.Introduction = note;
            existing.CreatedAtUtc = DateTime.UtcNow;
            existing.RespondedAtUtc = null;
            saved = await connections.UpdateAsync(existing, ContextOptions.ForCancellationToken(cancellationToken));
        }

        logger.LogInformation("User {UserId} asked to connect with {OtherUserId}", actor.Value.Id, other.Value.Id);
        await PublishConnectionAsync(saved, cancellationToken);
        return Result<ChatConversationDto>.Success(await ConversationAsync(actor.Value.Id, other.Value, cancellationToken));
    }

    /// <inheritdoc />
    public Task<Result<ChatConversationDto>> AcceptConnectionAsync(string userId, CancellationToken cancellationToken = default) =>
        RespondAsync(userId, accept: true, cancellationToken);

    /// <inheritdoc />
    public Task<Result<ChatConversationDto>> DeclineConnectionAsync(string userId, CancellationToken cancellationToken = default) =>
        RespondAsync(userId, accept: false, cancellationToken);

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> GetConnectedUserIdsAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.RequireConnections)
        {
            return Result<IReadOnlyList<string>>.Success([]);
        }

        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<IReadOnlyList<string>>();
        }

        return Result<IReadOnlyList<string>>.Success(await AcceptedIdsAsync(actor.Value.Id, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Result<int>> CountIncomingRequestsAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.RequireConnections)
        {
            return Result<int>.Success(0);
        }

        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<int>();
        }

        string userId = actor.Value.Id;
        var pending = await connections.FindAsync(new SearchOptions<ChatConnection>
        {
            Query = row => row.RecipientUserId == userId && row.Status == ChatConnectionState.Pending,
            CancellationToken = cancellationToken,
        });
        return Result<int>.Success(pending.Count);
    }

    private async Task<Result<IReadOnlyList<ChatConversationDto>>> ConnectedConversationsAsync(string userId, CancellationToken cancellationToken)
    {
        var rows = await connections.FindAsync(new SearchOptions<ChatConnection>
        {
            Query = row => (row.RequesterUserId == userId || row.RecipientUserId == userId) && row.Status != ChatConnectionState.Declined,
            CancellationToken = cancellationToken,
        });

        var ids = rows.Select(row => row.RequesterUserId == userId ? row.RecipientUserId : row.RequesterUserId).Distinct(StringComparer.Ordinal).ToList();
        var people = await directory.FindManyAsync(ids, cancellationToken);
        var unread = await UnreadCountsAsync(cancellationToken);
        var blockState = await BlocksBetweenAsync(userId, ids, cancellationToken);
        var list = new List<ChatConversationDto>();
        foreach (var row in rows)
        {
            string otherId = row.RequesterUserId == userId ? row.RecipientUserId : row.RequesterUserId;
            string name = people.TryGetValue(otherId, out var person) ? person.DisplayName : otherId;
            list.Add(new ChatConversationDto(
                otherId,
                name,
                unread.GetValueOrDefault(otherId),
                blockState.ByMe.Contains(otherId),
                blockState.ByThem.Contains(otherId),
                StatusFor(userId, row),
                row.Status == ChatConnectionState.Pending && row.Introduction.Length > 0 ? row.Introduction : null));
        }

        return Result<IReadOnlyList<ChatConversationDto>>.Success(list);
    }

    private async Task<Result<ChatConversationDto>> RespondAsync(string userId, bool accept, CancellationToken cancellationToken)
    {
        if (!options.Value.RequireConnections)
        {
            return Result<ChatConversationDto>.Forbidden("Connection requests are turned off.");
        }

        var actor = await RequireAsync(cancellationToken);
        if (!actor.IsSuccess)
        {
            return actor.To<ChatConversationDto>();
        }

        var other = await OtherPersonAsync(actor.Value.Id, userId, cancellationToken);
        if (!other.IsSuccess)
        {
            return other.To<ChatConversationDto>();
        }

        var row = await FindConnectionAsync(actor.Value.Id, other.Value.Id, cancellationToken);
        if (row is null || row.Status != ChatConnectionState.Pending || row.RecipientUserId != actor.Value.Id)
        {
            return Result<ChatConversationDto>.NotFound("There is no request from that person.");
        }

        row.Status = accept ? ChatConnectionState.Accepted : ChatConnectionState.Declined;
        row.RespondedAtUtc = DateTime.UtcNow;
        row = await connections.UpdateAsync(row, ContextOptions.ForCancellationToken(cancellationToken));

        if (accept && row.Introduction.Length > 0)
        {
            var saved = await messages.InsertAsync(new ChatMessage
            {
                Content = row.Introduction,
                Timestamp = DateTime.UtcNow,
                FromUserId = row.RequesterUserId,
                ToUserId = row.RecipientUserId,
            }, ContextOptions.ForCancellationToken(cancellationToken));
            await notifications.CreateAsync(saved.Id, row.RequesterUserId, row.RecipientUserId, cancellationToken);
            var requester = await directory.FindAsync(row.RequesterUserId, cancellationToken);
            var dto = ToMessage(saved, requester?.DisplayName ?? row.RequesterUserId, roomName: null);
            await realtime.PublishPrivateMessageAsync(dto, cancellationToken);
        }

        logger.LogInformation(
            "User {UserId} {Decision} the connection with {OtherUserId}",
            actor.Value.Id,
            accept ? "accepted" : "declined",
            other.Value.Id);
        await PublishConnectionAsync(row, cancellationToken);
        return Result<ChatConversationDto>.Success(await ConversationAsync(actor.Value.Id, other.Value, cancellationToken));
    }

    private async Task<Result<ChatUser>> OtherPersonAsync(string actorId, string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<ChatUser>.Invalid(new ValidationError(nameof(userId), "Choose someone to message."));
        }

        if (string.Equals(userId, actorId, StringComparison.Ordinal))
        {
            return Result<ChatUser>.Invalid(new ValidationError(nameof(userId), "You can't message yourself."));
        }

        var person = await directory.FindAsync(userId, cancellationToken);
        return person is null
            ? Result<ChatUser>.NotFound("That person could not be found.")
            : Result<ChatUser>.Success(person);
    }

    /// <summary>
    /// While connections are optional, a direct message is itself the connection.
    /// The row is already accepted, so turning the requirement on later does not separate people who were talking.
    /// </summary>
    private async Task RememberConnectionAsync(string fromUserId, string toUserId, CancellationToken cancellationToken)
    {
        if (options.Value.RequireConnections)
        {
            return;
        }

        var existing = await FindConnectionAsync(fromUserId, toUserId, cancellationToken);
        if (existing is { Status: ChatConnectionState.Accepted })
        {
            return;
        }

        if (existing is null)
        {
            await connections.InsertAsync(new ChatConnection
            {
                RequesterUserId = fromUserId,
                RecipientUserId = toUserId,
                Status = ChatConnectionState.Accepted,
                Introduction = string.Empty,
                CreatedAtUtc = DateTime.UtcNow,
                RespondedAtUtc = DateTime.UtcNow,
            }, ContextOptions.ForCancellationToken(cancellationToken));
            return;
        }

        existing.Status = ChatConnectionState.Accepted;
        existing.RespondedAtUtc = DateTime.UtcNow;
        await connections.UpdateAsync(existing, ContextOptions.ForCancellationToken(cancellationToken));
    }

    private async Task<ChatConnection?> FindConnectionAsync(string firstUserId, string secondUserId, CancellationToken cancellationToken) =>
        await connections.FindOneAsync(new SearchOptions<ChatConnection>
        {
            Query = row =>
                (row.RequesterUserId == firstUserId && row.RecipientUserId == secondUserId)
                || (row.RequesterUserId == secondUserId && row.RecipientUserId == firstUserId),
            CancellationToken = cancellationToken,
        });

    private async Task<List<string>> AcceptedIdsAsync(string userId, CancellationToken cancellationToken)
    {
        var rows = await connections.FindAsync(new SearchOptions<ChatConnection>
        {
            Query = row => row.Status == ChatConnectionState.Accepted && (row.RequesterUserId == userId || row.RecipientUserId == userId),
            CancellationToken = cancellationToken,
        });
        return rows
            .Select(row => row.RequesterUserId == userId ? row.RecipientUserId : row.RequesterUserId)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static ChatConnectionStatus StatusFor(string viewerId, ChatConnection? row)
    {
        if (row is null)
        {
            return ChatConnectionStatus.None;
        }

        return row.Status switch
        {
            ChatConnectionState.Accepted => ChatConnectionStatus.Accepted,
            ChatConnectionState.Declined => ChatConnectionStatus.Declined,
            _ => row.RequesterUserId == viewerId ? ChatConnectionStatus.PendingOutgoing : ChatConnectionStatus.PendingIncoming,
        };
    }

    private async Task PublishConnectionAsync(ChatConnection row, CancellationToken cancellationToken)
    {
        var change = new ChatConnectionChangedDto(row.RequesterUserId, row.RecipientUserId, row.Status, row.Introduction);
        await realtime.PublishConnectionChangedAsync(change, cancellationToken);
        await realtime.PublishUnreadChangedAsync(row.RequesterUserId, cancellationToken);
        await realtime.PublishUnreadChangedAsync(row.RecipientUserId, cancellationToken);
    }
}
