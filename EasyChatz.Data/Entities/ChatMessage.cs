using System.Diagnostics;

namespace EasyChatz.Data.Entities;

[DebuggerDisplay("{FromUserId}: {Content}")]
public class ChatMessage : BaseEntity<int>
{
    public string Content { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string FromUserId { get; set; } = string.Empty;

    /// <summary>Set for room messages. Null for a direct message.</summary>
    public int? ToRoomId { get; set; }

    /// <summary>Set for a direct message. Null for a room message.</summary>
    public string? ToUserId { get; set; }

    public virtual ChatRoom? ToRoom { get; set; }

    public virtual ICollection<MessageNotification> Notifications { get; set; } = [];
}

public class ChatMessageMap : IEntityTypeConfiguration<ChatMessage>
{
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
