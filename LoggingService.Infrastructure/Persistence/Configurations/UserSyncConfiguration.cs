using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoggingService.Domain.Entities;
namespace LoggingService.Infrastructure.Persistence.Configurations;

public class UserSyncConfiguration : IEntityTypeConfiguration<UserSync>
{
    public void Configure(EntityTypeBuilder<UserSync> builder)
    {
        builder.ToTable("usersyncs");
        builder.HasKey(e => e.UserId);
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(450);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.BranchId);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
        builder.Property(e => e.UserName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.FirstName).HasMaxLength(100);
        builder.Property(e => e.LastName).HasMaxLength(100);
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.IsSuperAdmin).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.Roles).IsRequired().HasDefaultValue("[]");
        builder.Property(e => e.LastSyncedAt).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.HasIndex(e => new { e.TenantId, e.IsActive, e.IsDeleted })
            .HasDatabaseName("ix_usersyncs_tenantid_isactive_isdeleted");
        builder.HasIndex(e => e.Email)
            .HasDatabaseName("ix_usersyncs_email");
    }
}
