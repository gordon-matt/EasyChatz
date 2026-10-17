using Microsoft.EntityFrameworkCore.Design;

namespace EasyChatz.Data.Sqlite;

/// <summary>Used by <c>dotnet ef</c> so migrations can be generated without a host application.</summary>
public class EasyChatzDbContextDesignTimeFactory : IDesignTimeDbContextFactory<EasyChatzDbContext>
{
    public EasyChatzDbContext CreateDbContext(string[] args) =>
        new(EasyChatzDbContextFactory.BuildOptions("Data Source=easychatz_design.db"));
}
