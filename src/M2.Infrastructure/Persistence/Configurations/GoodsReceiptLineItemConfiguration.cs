using M2.Domain.GoodsReceipt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class GoodsReceiptLineItemConfiguration : BaseEntityConfiguration<GoodsReceiptLineItem>
{
    public override void Configure(EntityTypeBuilder<GoodsReceiptLineItem> builder)
    {
        builder.ToTable("goods_receipt_line_items");
        builder.HasKey(li => li.Id);
        builder.Property(li => li.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(li => li.GoodsReceiptNoteId)
            .IsRequired()
            .HasColumnType("uuid")
            .HasColumnName("GoodsReceiptNoteId");

        builder.Property(li => li.ProductCode)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("ProductCode");

        builder.OwnsOneBilingual(li => li.ProductName);

        builder.Property(li => li.ExpectedQty)
            .IsRequired()
            .HasColumnType("numeric(18,4)")
            .HasColumnName("ExpectedQty");

        builder.Property(li => li.ReceivedQty)
            .IsRequired()
            .HasColumnType("numeric(18,4)")
            .HasColumnName("ReceivedQty");

        builder.Property(li => li.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("UnitOfMeasure");

        builder.Property(li => li.DiscrepancyNote)
            .HasMaxLength(500)
            .HasColumnName("DiscrepancyNote");

        builder.HasOne<GoodsReceiptNote>()
            .WithMany(g => g.LineItems)
            .HasForeignKey(li => li.GoodsReceiptNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(li => li.GoodsReceiptNoteId)
            .HasDatabaseName("IX_goods_receipt_line_items_GoodsReceiptNoteId");

        builder.HasQueryFilter(li => !li.IsDeleted);
    }
}
