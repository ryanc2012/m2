using M2.Domain.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class PromotionConfiguration : BaseEntityConfiguration<Promotion>
{
    public override void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.OwnsOneBilingual(p => p.Name);

        builder.Property(p => p.Type)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Type")
            .HasConversion<string>();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Status")
            .HasConversion<string>();

        builder.Property(p => p.FormulaJson)
            .IsRequired()
            .HasColumnType("text")
            .HasColumnName("FormulaJson");

        builder.Property(p => p.StartDate)
            .IsRequired()
            .HasColumnName("StartDate")
            .HasColumnType("timestamptz");

        builder.Property(p => p.EndDate)
            .IsRequired()
            .HasColumnName("EndDate")
            .HasColumnType("timestamptz");

        builder.Property(p => p.IsStackable)
            .IsRequired()
            .HasColumnName("IsStackable");

        builder.Property(p => p.ApprovalRequestId)
            .HasColumnType("uuid")
            .HasColumnName("ApprovalRequestId");

        builder.HasIndex(p => new { p.TenantId, p.ShopId, p.Status })
            .HasDatabaseName("IX_promotions_TenantId_ShopId_Status");

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
