namespace EasyChatz.Data.MySql;

public class EasyChatzDbContext(DbContextOptions<EasyChatzDbContext> options) : EasyChatzDbContextBase(options);
