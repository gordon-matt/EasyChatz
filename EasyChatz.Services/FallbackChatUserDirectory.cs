namespace EasyChatz.Services;

/// <summary>
/// Addresses people by their id when the host has not supplied a directory. Administrators and
/// room creation then come only from claims, once <c>AddEasyChatz()</c> is used.
/// </summary>
internal sealed class FallbackChatUserDirectory : IChatUserDirectory
{
    public Task<ChatUser?> FindAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ChatUser?>(string.IsNullOrWhiteSpace(userId) ? null : Synthetic(userId));

    public Task<IReadOnlyDictionary<string, ChatUser>> FindManyAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var found = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(id => id, Synthetic, StringComparer.Ordinal);

        return Task.FromResult<IReadOnlyDictionary<string, ChatUser>>(found);
    }

    private static ChatUser Synthetic(string userId) => new(userId, userId, false, true);
}

/// <summary>Replaced by the ASP.NET Core host, which reads the signed-in principal.</summary>
internal sealed class UnauthenticatedChatUser : IChatCurrentUser
{
    public Task<ChatUser?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult<ChatUser?>(null);
}
