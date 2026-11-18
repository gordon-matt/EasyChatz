using EasyChatz.Models;

namespace EasyChatz.Data.Entities;

/// <summary>
/// A request to exchange direct messages. Stored once per pair. Room messages do not use this.
/// </summary>
public class ChatConnection : BaseEntity<int>
{
    /// <summary>Host tenant key. Empty for a single-tenant host.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>The person who asked.</summary>
    public string RequesterUserId { get; set; } = string.Empty;

    /// <summary>The person who was asked.</summary>
    public string RecipientUserId { get; set; } = string.Empty;

    /// <summary>Pending until the recipient accepts or declines.</summary>
    public ChatConnectionState Status { get; set; } = ChatConnectionState.Pending;

    /// <summary>Optional note shown with the request. Empty when they asked without one.</summary>
    public string Introduction { get; set; } = string.Empty;

    /// <summary>When the request was created or asked again, in UTC.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When it was accepted or declined, in UTC. Null while it is pending.</summary>
    public DateTime? RespondedAtUtc { get; set; }
}

/// <summary>Maps <see cref="ChatConnection"/>.</summary>
public class ChatConnectionMap : IEntityTypeConfiguration<ChatConnection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChatConnection> builder)
    {
        builder.ToTable(Constants.Tables.Connections, Constants.Schema);
        builder.HasKey(row => row.Id);
        builder.Property(row => row.TenantId).IsRequired().HasMaxLength(Constants.MaxLengths.TenantId).IsUnicode(false).HasDefaultValue(string.Empty);
        builder.Property(row => row.RequesterUserId).IsRequired().HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(false);
        builder.Property(row => row.RecipientUserId).IsRequired().HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(false);
        builder.Property(row => row.Status).HasConversion<int>();
        builder.Property(row => row.Introduction).IsRequired().HasMaxLength(Constants.MaxLengths.Message).IsUnicode(true);
        builder.Property(row => row.CreatedAtUtc).HasConversion(UtcDateTimeConverter.Instance);
        builder.Property(row => row.RespondedAtUtc).HasConversion(UtcDateTimeConverter.Instance);

        builder.HasIndex(row => new { row.TenantId, row.RequesterUserId, row.RecipientUserId }).IsUnique();
        builder.HasIndex(row => new { row.TenantId, row.RecipientUserId });
    }
}
