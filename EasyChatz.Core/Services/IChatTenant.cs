namespace EasyChatz.Services;

/// <summary>
/// The current tenant for chat data. EasyChatz does not reference a tenancy library.
/// A single-tenant host registers nothing, and every row uses an empty tenant id.
/// A multi-tenant host registers its own implementation and returns the key that library already resolved.
/// </summary>
public interface IChatTenant
{
    /// <summary>
    /// The current tenant key, or null when this call belongs to the single-tenant partition.
    /// The same user id in two tenants is two separate inboxes. A key is at most 64 characters.
    /// </summary>
    string? TenantId { get; }
}
