using M2.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class DeviceRegistrationConfiguration : BaseEntityConfiguration<DeviceRegistration>
{
    public override void Configure(EntityTypeBuilder<DeviceRegistration> builder)
    {
        builder.ToTable("device_registrations");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(d => d.UserId)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("UserId");

        builder.Property(d => d.Platform)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("Platform")
            .HasConversion<string>();

        builder.Property(d => d.FcmToken)
            .HasMaxLength(512)
            .HasColumnName("FcmToken");

        builder.Property(d => d.ApnsToken)
            .HasMaxLength(512)
            .HasColumnName("ApnsToken");

        builder.Property(d => d.RegisteredAt)
            .IsRequired()
            .HasColumnName("RegisteredAt")
            .HasColumnType("timestamptz");

        builder.HasIndex(d => new { d.TenantId, d.UserId, d.Platform })
            .IsUnique()
            .HasDatabaseName("IX_device_registrations_TenantId_UserId_Platform");

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
