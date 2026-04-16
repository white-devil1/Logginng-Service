using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoggingService.Domain.Entities;
namespace LoggingService.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activitylogs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Timestamp).IsRequired();
        builder.Property(e => e.ActionType).IsRequired();
        builder.Property(e => e.EntityType).IsRequired();
        builder.Property(e => e.EntityId).HasMaxLength(450);
        builder.Property(e => e.ServiceName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).IsRequired();
        builder.Property(e => e.UserId).HasMaxLength(450);
        builder.Property(e => e.UserEmail).HasMaxLength(256);
        builder.Property(e => e.UserRole).HasMaxLength(100);
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.Property(e => e.CreatedAt).IsRequired()
            .HasDefaultValueSql("NOW()");
        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("ix_activitylogs_timestamp");
        builder.HasIndex(e => new { e.TenantId, e.Timestamp })
            .HasDatabaseName("ix_activitylogs_tenantid_timestamp");
        builder.HasIndex(e => e.ActionType)
            .HasDatabaseName("ix_activitylogs_actiontype");
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_activitylogs_userid");
    }
}
