namespace EasyChatz.Data.Entities;

/// <summary>
/// One person has stopped another from sending them direct messages. Room messages are not affected,
/// and the block can be removed.
/// </summary>
public class ChatUserBlock : BaseEntity<int>
{
    /// <summary>The person who created the block.</summary>
    public string BlockerUserId { get; set; } = string.Empty;

    /// <summary>The person who can no longer send direct messages to <see cref="BlockerUserId"/>.</summary>
    public string BlockedUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the created at utc.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the chat user block map.
/// </summary>
public class ChatUserBlockMap : IEntityTypeConfiguration<ChatUserBlock>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<ChatUserBlock> builder)
    {
        builder.ToTable(Constants.Tables.UserBlocks, Constants.Schema);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.BlockerUserId).IsRequired().HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(false);
        builder.Property(m => m.BlockedUserId).IsRequired().HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(false);
        builder.Property(m => m.CreatedAtUtc).HasConversion(UtcDateTimeConverter.Instance);

        // One row per pair. The second index serves "who has blocked me".
        builder.HasIndex(m => new { m.BlockerUserId, m.BlockedUserId }).IsUnique();
        builder.HasIndex(m => m.BlockedUserId);
    }
}
