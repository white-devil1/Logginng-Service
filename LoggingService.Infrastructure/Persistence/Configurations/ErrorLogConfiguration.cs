using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoggingService.Domain.Entities;
namespace LoggingService.Infrastructure.Persistence.Configurations;

public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.ToTable("errorlogs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Timestamp).IsRequired();
        builder.Property(e => e.Severity).IsRequired();
        builder.Property(e => e.Source).IsRequired();
        builder.Property(e => e.Category).IsRequired();
        builder.Property(e => e.ServiceName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Environment).HasMaxLength(50);
        builder.Property(e => e.Message).IsRequired();
        builder.Property(e => e.RequestPath).HasMaxLength(500);
        builder.Property(e => e.RequestMethod).HasMaxLength(10);
        builder.Property(e => e.UserId).HasMaxLength(450);
        builder.Property(e => e.UserEmail).HasMaxLength(256);
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.Property(e => e.CreatedAt).IsRequired()
            .HasDefaultValueSql("NOW()");
        // Indexes for filter performance
        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("ix_errorlogs_timestamp");
        builder.HasIndex(e => new { e.TenantId, e.Timestamp })
            .HasDatabaseName("ix_errorlogs_tenantid_timestamp");
        builder.HasIndex(e => e.ServiceName)
            .HasDatabaseName("ix_errorlogs_servicename");
        builder.HasIndex(e => e.Category)
            .HasDatabaseName("ix_errorlogs_category");
        builder.HasIndex(e => e.Severity)
            .HasDatabaseName("ix_errorlogs_severity");
    }
}
