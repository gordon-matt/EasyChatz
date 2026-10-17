using Microsoft.EntityFrameworkCore.Design;

namespace EasyChatz.Data.Npgsql;

/// <summary>Used by <c>dotnet ef</c> so migrations can be generated without a running PostgreSQL server.</summary>
public class EasyChatzDbContextDesignTimeFactory : IDesignTimeDbContextFactory<EasyChatzDbContext>
{
    public EasyChatzDbContext CreateDbContext(string[] args) =>
        new(EasyChatzDbContextFactory.BuildOptions(
            "Host=localhost;Database=easychatz_design;Username=postgres;Password=postgres"));
}
