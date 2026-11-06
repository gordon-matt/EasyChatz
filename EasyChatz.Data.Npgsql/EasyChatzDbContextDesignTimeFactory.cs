using Microsoft.EntityFrameworkCore.Design;

namespace EasyChatz.Data.Npgsql;

/// <summary>Used by <c>dotnet ef</c> so migrations can be generated without a running PostgreSQL server.</summary>
public class EasyChatzDbContextDesignTimeFactory : IDesignTimeDbContextFactory<EasyChatzDbContext>
{
    /// <summary>
    /// Creates the database context.
    /// </summary>
    /// <param name="args">The args.</param>
    /// <returns>The database context.</returns>
    public EasyChatzDbContext CreateDbContext(string[] args) =>
        new(EasyChatzDbContextFactory.BuildOptions(
            "Host=localhost;Database=easychatz_design;Username=postgres;Password=postgres"));
}
