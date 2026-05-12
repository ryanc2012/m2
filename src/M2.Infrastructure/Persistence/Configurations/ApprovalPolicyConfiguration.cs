using M2.Domain.Approvals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class ApprovalPolicyConfiguration : BaseEntityConfiguration<ApprovalPolicy>
{
    public override void Configure(EntityTypeBuilder<ApprovalPolicy> builder)
    {
        builder.ToTable("approval_policies");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(p => p.EntityType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("EntityType");

        builder.Property(p => p.Mode)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Mode")
            .HasConversion<string>();

        builder.Property(p => p.MaxLevels)
            .IsRequired()
            .HasDefaultValue(2)
            .HasColumnName("MaxLevels");

        builder.HasIndex(p => new { p.TenantId, p.EntityType })
            .IsUnique()
            .HasDatabaseName("IX_approval_policies_TenantId_EntityType");

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
