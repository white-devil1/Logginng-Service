using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using LoggingService.Domain.Entities;
namespace LoggingService.Infrastructure.Persistence;

public class LoggingDbContext : DbContext
{
    public LoggingDbContext(DbContextOptions<LoggingDbContext> options)
        : base(options) { }

    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<LoginAudit> LoginAudits => Set<LoginAudit>();
    public DbSet<UserSync> UserSyncs => Set<UserSync>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(LoggingDbContext).Assembly);
    }
}

// For EF migrations
public class LoggingDbContextFactory
    : IDesignTimeDbContextFactory<LoggingDbContext>
{
    public LoggingDbContext CreateDbContext(string[] args)
    {
        var ob = new DbContextOptionsBuilder<LoggingDbContext>();
        ob.UseNpgsql(
            "Host=localhost;Port=5432;Database=LoggingDb;" +
            "Username=postgres;Password=Aneesh@123");
        return new LoggingDbContext(ob.Options);
    }
}
