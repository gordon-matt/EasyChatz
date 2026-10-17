namespace EasyChatz.Data.MySql;

public class EasyChatzDbContextFactory(string connectionString) : IEasyChatzDbContextFactory
{
    private DbContextOptions<EasyChatzDbContext> Options => field ??= BuildOptions(connectionString);

    public string ProviderName => Constants.DatabaseProviders.MySql;

    public DbContext GetContext() => new EasyChatzDbContext(Options);

    public DbContext GetContext(string connectionString) => new EasyChatzDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<EasyChatzDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EasyChatzDbContext>();

        optionsBuilder.UseMySQL(connectionString, mySql => mySql
            .MigrationsAssembly(typeof(EasyChatzDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable, Constants.Schema));

        return optionsBuilder.Options;
    }
}
