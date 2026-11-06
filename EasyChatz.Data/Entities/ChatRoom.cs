using System.Diagnostics;

namespace EasyChatz.Data.Entities;

/// <summary>
/// Represents the chat room.
/// </summary>
[DebuggerDisplay("{Name}")]
public class ChatRoom : BaseEntity<int>
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the admin user identifier.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the created at utc.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the messages.
    /// </summary>
    public virtual ICollection<ChatMessage> Messages { get; set; } = [];
}

/// <summary>
/// Represents the chat room map.
/// </summary>
public class ChatRoomMap : IEntityTypeConfiguration<ChatRoom>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<ChatRoom> builder)
    {
        builder.ToTable(Constants.Tables.Rooms, Constants.Schema);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(Constants.MaxLengths.RoomName).IsUnicode(true);
        builder.Property(m => m.AdminUserId).IsRequired().HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(false);
        builder.Property(m => m.CreatedAtUtc).HasConversion(UtcDateTimeConverter.Instance);
        builder.HasIndex(m => m.Name).IsUnique();
    }
}
