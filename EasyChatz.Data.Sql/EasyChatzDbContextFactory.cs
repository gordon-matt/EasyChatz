namespace EasyChatz.Data.Sql;

/// <summary>
/// Represents the easy chatz database context factory.
/// </summary>
public class EasyChatzDbContextFactory(string connectionString) : IEasyChatzDbContextFactory
{
    private DbContextOptions<EasyChatzDbContext> Options => field ??= BuildOptions(connectionString);

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string ProviderName => Constants.DatabaseProviders.SqlServer;

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <returns>The context.</returns>
    public DbContext GetContext() => new EasyChatzDbContext(Options);

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The context.</returns>
    public DbContext GetContext(string connectionString) => new EasyChatzDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<EasyChatzDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EasyChatzDbContext>();

        // The loaded assembly, not its name. A plugin is not on the host's load path, so
        // Assembly.Load of the name cannot see these migrations and the tables are never created.
        optionsBuilder.UseSqlServer(connectionString, sql => sql
            .MigrationsAssembly(typeof(EasyChatzDbContext).Assembly)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable, Constants.Schema));

        return optionsBuilder.Options;
    }
}
