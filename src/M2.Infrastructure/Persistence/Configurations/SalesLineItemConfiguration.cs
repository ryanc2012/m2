using M2.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class SalesLineItemConfiguration : BaseEntityConfiguration<SalesLineItem>
{
    public override void Configure(EntityTypeBuilder<SalesLineItem> builder)
    {
        builder.ToTable("sales_line_items");
        builder.HasKey(li => li.Id);
        builder.Property(li => li.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(li => li.TransactionId)
            .IsRequired()
            .HasColumnType("uuid")
            .HasColumnName("TransactionId");

        builder.Property(li => li.ProductId)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("ProductId");

        // Bilingual product name snapshot — stored as individual columns (ProductName_en / ProductName_zht)
        builder.Property(li => li.ProductNameEn)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("ProductName_en");

        builder.Property(li => li.ProductNameZht)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("ProductName_zht");

        builder.Property(li => li.Quantity)
            .IsRequired()
            .HasColumnName("Quantity");

        builder.Property(li => li.UnitPrice)
            .IsRequired()
            .HasColumnType("numeric(18,2)")
            .HasColumnName("UnitPrice");

        builder.Property(li => li.DiscountAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)")
            .HasColumnName("DiscountAmount");

        builder.Property(li => li.LineTotal)
            .IsRequired()
            .HasColumnType("numeric(18,2)")
            .HasColumnName("LineTotal");

        builder.HasOne<SalesTransaction>()
            .WithMany(st => st.LineItems)
            .HasForeignKey(li => li.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(li => !li.IsDeleted);
    }
}
