namespace EasyChatz.Services;

/// <summary>
/// Used when the host has not registered an <see cref="IChatTenant"/>.
/// Every row is stored with an empty tenant id.
/// </summary>
public sealed class DefaultChatTenant : IChatTenant
{
    /// <inheritdoc />
    public string? TenantId => null;
}
