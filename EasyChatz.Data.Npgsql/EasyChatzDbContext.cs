namespace EasyChatz.Data.Npgsql;

public class EasyChatzDbContext(DbContextOptions<EasyChatzDbContext> options) : EasyChatzDbContextBase(options);
