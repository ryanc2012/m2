using M2.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class MemberConfiguration : BaseEntityConfiguration<Member>
{
    public override void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.OwnsOneBilingual(m => m.FirstName);
        builder.OwnsOneBilingual(m => m.LastName);

        builder.Property(m => m.Phone)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("Phone");

        builder.Property(m => m.Email)
            .HasMaxLength(256)
            .HasColumnName("Email");

        builder.Property(m => m.QrCode)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("QrCode");

        builder.Property(m => m.MembershipTier)
            .HasMaxLength(50)
            .HasColumnName("MembershipTier")
            .HasConversion<string>();

        builder.Property(m => m.JoinedAt)
            .IsRequired()
            .HasColumnName("JoinedAt")
            .HasColumnType("timestamptz");

        builder.Property(m => m.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("IsActive");

        builder.HasIndex(m => new { m.TenantId, m.Phone })
            .IsUnique()
            .HasDatabaseName("IX_members_TenantId_Phone");

        builder.HasIndex(m => m.QrCode)
            .IsUnique()
            .HasDatabaseName("IX_members_QrCode");

        builder.HasIndex(m => new { m.TenantId, m.ShopId })
            .HasDatabaseName("IX_members_TenantId_ShopId");

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
