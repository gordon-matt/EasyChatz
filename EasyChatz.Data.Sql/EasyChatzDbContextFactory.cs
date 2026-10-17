namespace EasyChatz.Data.Sql;

public class EasyChatzDbContextFactory(string connectionString) : IEasyChatzDbContextFactory
{
    private DbContextOptions<EasyChatzDbContext> Options => field ??= BuildOptions(connectionString);

    public string ProviderName => Constants.DatabaseProviders.SqlServer;

    public DbContext GetContext() => new EasyChatzDbContext(Options);

    public DbContext GetContext(string connectionString) => new EasyChatzDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<EasyChatzDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EasyChatzDbContext>();

        optionsBuilder.UseSqlServer(connectionString, sql => sql
            .MigrationsAssembly(typeof(EasyChatzDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable, Constants.Schema));

        return optionsBuilder.Options;
    }
}
