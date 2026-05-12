using M2.Domain.GoodsReceipt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class GoodsReceiptNoteConfiguration : BaseEntityConfiguration<GoodsReceiptNote>
{
    public override void Configure(EntityTypeBuilder<GoodsReceiptNote> builder)
    {
        builder.ToTable("goods_receipt_notes");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(g => g.SapDeliveryNoteNumber)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("SapDeliveryNoteNumber");

        builder.Property(g => g.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Status")
            .HasConversion<string>();

        builder.Property(g => g.ReceivedAt)
            .HasColumnName("ReceivedAt")
            .HasColumnType("timestamptz");

        builder.Property(g => g.ConfirmedAt)
            .HasColumnName("ConfirmedAt")
            .HasColumnType("timestamptz");

        builder.HasIndex(g => new { g.TenantId, g.ShopId, g.IsDeleted })
            .HasDatabaseName("IX_goods_receipt_notes_TenantId_ShopId_IsDeleted");

        builder.HasIndex(g => new { g.TenantId, g.ShopId, g.Status })
            .HasDatabaseName("IX_goods_receipt_notes_TenantId_ShopId_Status");

        builder.HasQueryFilter(g => !g.IsDeleted);
    }
}
