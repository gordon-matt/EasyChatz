namespace EasyChatz.Data.Npgsql;

/// <summary>
/// Represents the easy chatz database context.
/// </summary>
public class EasyChatzDbContext(DbContextOptions<EasyChatzDbContext> options) : EasyChatzDbContextBase(options);
