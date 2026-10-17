using System.Diagnostics;

namespace EasyChatz.Data.Entities;

[DebuggerDisplay("{Name}")]
public class ChatRoom : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;

    public string AdminUserId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual ICollection<ChatMessage> Messages { get; set; } = [];
}

public class ChatRoomMap : IEntityTypeConfiguration<ChatRoom>
{
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
