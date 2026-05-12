using M2.Domain.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class CouponConfiguration : BaseEntityConfiguration<Coupon>
{
    public override void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(c => c.PromotionId)
            .IsRequired()
            .HasColumnType("uuid")
            .HasColumnName("PromotionId");

        builder.Property(c => c.MemberId)
            .HasColumnType("uuid")
            .HasColumnName("MemberId");

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Code");

        builder.Property(c => c.IssuedAt)
            .IsRequired()
            .HasColumnName("IssuedAt")
            .HasColumnType("timestamptz");

        builder.Property(c => c.RedeemedAt)
            .HasColumnName("RedeemedAt")
            .HasColumnType("timestamptz");

        builder.Property(c => c.ExpiresAt)
            .IsRequired()
            .HasColumnName("ExpiresAt")
            .HasColumnType("timestamptz");

        builder.Property(c => c.IsRedeemed)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("IsRedeemed");

        // FK → promotions (same module, cascade delete)
        builder.HasOne<Promotion>()
            .WithMany(p => p.Coupons)
            .HasForeignKey(c => c.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasDatabaseName("IX_coupons_Code");

        builder.HasIndex(c => new { c.PromotionId, c.IsRedeemed })
            .HasDatabaseName("IX_coupons_PromotionId_IsRedeemed");

        builder.HasIndex(c => new { c.MemberId, c.IsRedeemed })
            .HasDatabaseName("IX_coupons_MemberId_IsRedeemed");

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
