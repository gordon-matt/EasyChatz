using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EasyChatz.Data.Sqlite;

public class EasyChatzDbContextFactory(string connectionString) : IEasyChatzDbContextFactory
{
    private DbContextOptions<EasyChatzDbContext> Options => field ??= BuildOptions(connectionString);

    public string ProviderName => Constants.DatabaseProviders.Sqlite;

    public DbContext GetContext() => new EasyChatzDbContext(Options);

    public DbContext GetContext(string connectionString) => new EasyChatzDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<EasyChatzDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EasyChatzDbContext>();

        optionsBuilder.UseSqlite(Prepare(connectionString), sqlite => sqlite
            .MigrationsAssembly(typeof(EasyChatzDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable, Constants.Schema));

        // SQLite has no schemas. HasDefaultSchema("chatz") only names the tables; the warning is noise.
        optionsBuilder.ConfigureWarnings(w => w.Ignore(SqliteEventId.SchemaConfiguredWarning));

        return optionsBuilder.Options;
    }

    /// <summary>
    /// Creates the database's folder, since SQLite will create a missing file but not a missing
    /// directory, and forces foreign keys on so deleting a room cascades to its messages.
    /// </summary>
    private static string Prepare(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true };

        if (builder.DataSource is { Length: > 0 } dataSource
            && !dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            && builder.Mode != SqliteOpenMode.Memory
            && Path.GetDirectoryName(Path.GetFullPath(dataSource)) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        return builder.ToString();
    }
}
