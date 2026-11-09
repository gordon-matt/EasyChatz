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

    /// <summary>
    /// Gets the user blocks.
    /// </summary>
    public DbSet<ChatUserBlock> UserBlocks => Set<ChatUserBlock>();

    /// <summary>
    /// Whether the provider can hold the tables in <see cref="Constants.Schema"/>. Providers where a
    /// schema is a database (MySQL) or does not exist (SQLite) return <see langword="false"/>, and
    /// get <see cref="Constants.TablePrefix"/> on every table name instead.
    /// </summary>
    protected virtual bool SupportsSchemas => true;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(EasyChatzDbContextBase).Assembly);

        if (SupportsSchemas)
        {
            builder.HasDefaultSchema(Constants.Schema);
            return;
        }

        builder.Model.SetDefaultSchema(null);

        foreach (var entityType in builder.Model.GetEntityTypes().Where(e => e.BaseType is null))
        {
            if (entityType.GetTableName() is not { } tableName)
            {
                continue;
            }

            entityType.SetSchema(null);
            entityType.SetTableName($"{Constants.TablePrefix}{tableName}");
        }
    }
}
