namespace EasyChatz.Data;

public abstract class EasyChatzDbContextBase(DbContextOptions options) : DbContext(options)
{
    public DbSet<ChatRoom> Rooms => Set<ChatRoom>();

    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    public DbSet<MessageNotification> Notifications => Set<MessageNotification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Constants.Schema);
        builder.ApplyConfigurationsFromAssembly(typeof(EasyChatzDbContextBase).Assembly);
    }
}
