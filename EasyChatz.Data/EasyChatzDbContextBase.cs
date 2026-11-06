namespace EasyChatz.Data;

/// <summary>
/// Represents the easy chatz database context base.
/// </summary>
public abstract class EasyChatzDbContextBase(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// Gets the rooms.
    /// </summary>
    public DbSet<ChatRoom> Rooms => Set<ChatRoom>();

    /// <summary>
    /// Gets the messages.
    /// </summary>
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    /// <summary>
    /// Gets the notifications.
    /// </summary>
    public DbSet<MessageNotification> Notifications => Set<MessageNotification>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Constants.Schema);
        builder.ApplyConfigurationsFromAssembly(typeof(EasyChatzDbContextBase).Assembly);
    }
}
