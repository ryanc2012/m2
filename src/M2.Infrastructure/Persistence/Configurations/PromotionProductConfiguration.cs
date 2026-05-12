using M2.Domain.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class PromotionProductConfiguration : IEntityTypeConfiguration<PromotionProduct>
{
    public void Configure(EntityTypeBuilder<PromotionProduct> builder)
    {
        builder.ToTable("promotion_products");
        builder.HasKey(pp => new { pp.PromotionId, pp.ProductId });

        builder.Property(pp => pp.PromotionId)
            .IsRequired()
            .HasColumnType("uuid")
            .HasColumnName("PromotionId");

        builder.Property(pp => pp.ProductId)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("ProductId");

        builder.Property(pp => pp.DiscountValue)
            .IsRequired()
            .HasColumnType("numeric(18,2)")
            .HasColumnName("DiscountValue");

        builder.HasOne<Promotion>()
            .WithMany(p => p.Products)
            .HasForeignKey(pp => pp.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
