using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoggingService.Domain.Entities;
using LoggingService.Domain.Enums;
namespace LoggingService.Infrastructure.Persistence.Configurations;

public class LoginAuditConfiguration : IEntityTypeConfiguration<LoginAudit>
{
    public void Configure(EntityTypeBuilder<LoginAudit> builder)
    {
        builder.ToTable("loginaudits");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Timestamp).IsRequired();
        builder.Property(e => e.EventType).IsRequired();
        builder.Property(e => e.UserId).HasMaxLength(450);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.DeviceType).IsRequired().HasDefaultValue(DeviceType.Unknown);
        builder.Property(e => e.CreatedAt).IsRequired()
            .HasDefaultValueSql("NOW()");
        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("ix_loginaudits_timestamp");
        builder.HasIndex(e => new { e.TenantId, e.Timestamp })
            .HasDatabaseName("ix_loginaudits_tenantid_timestamp");
        builder.HasIndex(e => e.Email)
            .HasDatabaseName("ix_loginaudits_email");
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_loginaudits_userid");
    }
}