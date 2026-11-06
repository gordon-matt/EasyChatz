using System.Diagnostics;

namespace EasyChatz.Data.Entities;

/// <summary>
/// Represents the chat message.
/// </summary>
[DebuggerDisplay("{FromUserId}: {Content}")]
public class ChatMessage : BaseEntity<int>
{
    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the from user identifier.
    /// </summary>
    public string FromUserId { get; set; } = string.Empty;

    /// <summary>Set for room messages. Null for a direct message.</summary>
    public int? ToRoomId { get; set; }

    /// <summary>Set for a direct message. Null for a room message.</summary>
    public string? ToUserId { get; set; }

    /// <summary>
    /// Gets or sets the to room.
    /// </summary>
    public virtual ChatRoom? ToRoom { get; set; }

    /// <summary>
    /// Gets or sets the notifications.
    /// </summary>
    public virtual ICollection<MessageNotification> Notifications { get; set; } = [];
}

/// <summary>
/// Represents the chat message map.
/// </summary>
public class ChatMessageMap : IEntityTypeConfiguration<ChatMessage>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable(Constants.Tables.Messages, Constants.Schema);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Content).IsRequired().HasMaxLength(Constants.MaxLengths.Message).IsUnicode(true);
        builder.Property(m => m.FromUserId).IsRequired().HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(false);
        builder.Property(m => m.ToUserId).HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(false);
        builder.Property(m => m.Timestamp).HasConversion(UtcDateTimeConverter.Instance);

        builder.HasOne(m => m.ToRoom)
            .WithMany(m => m.Messages)
            .HasForeignKey(m => m.ToRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ToRoomId, m.Id });
        builder.HasIndex(m => new { m.FromUserId, m.ToUserId, m.Id });
        builder.HasIndex(m => new { m.ToUserId, m.FromUserId, m.Id });
    }
}
