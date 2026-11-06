namespace EasyChatz.Data.Entities;

/// <summary>One row per direct message, so a recipient can see what they have not opened yet.</summary>
public class MessageNotification : BaseEntity<int>
{
    /// <summary>
    /// Gets or sets the chat message identifier.
    /// </summary>
    public int ChatMessageId { get; set; }

    /// <summary>
    /// Gets or sets the from user identifier.
    /// </summary>
    public string FromUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the to user identifier.
    /// </summary>
    public string ToUserId { get; set; } = string.Empty;

    /// <summary>True once the recipient opens that conversation.</summary>
    public bool IsRead { get; set; }

    /// <summary>Set when a host channel has taken this notice. Null while it is still waiting.</summary>
    public DateTime? DeliveredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the created at utc.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public virtual ChatMessage Message { get; set; } = null!;
}

/// <summary>
/// Represents the message notification map.
/// </summary>
public class MessageNotificationMap : IEntityTypeConfiguration<MessageNotification>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<MessageNotification> builder)
    {
        builder.ToTable(Constants.Tables.Notifications, Constants.Schema);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.FromUserId).IsRequired().HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(false);
        builder.Property(m => m.ToUserId).IsRequired().HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(false);
        builder.Property(m => m.CreatedAtUtc).HasConversion(UtcDateTimeConverter.Instance);
        builder.Property(m => m.DeliveredAtUtc).HasConversion(UtcDateTimeConverter.Instance);

        builder.HasOne(m => m.Message)
            .WithMany(m => m.Notifications)
            .HasForeignKey(m => m.ChatMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ToUserId, m.FromUserId, m.IsRead });
        builder.HasIndex(m => new { m.IsRead, m.DeliveredAtUtc });
    }
}
