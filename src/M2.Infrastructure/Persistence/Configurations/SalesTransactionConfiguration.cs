using M2.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class SalesTransactionConfiguration : BaseEntityConfiguration<SalesTransaction>
{
    public override void Configure(EntityTypeBuilder<SalesTransaction> builder)
    {
        builder.ToTable("sales_transactions");
        builder.HasKey(st => st.Id);
        builder.Property(st => st.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        // Cross-module reference to members — no navigation property (ADR-001)
        builder.Property(st => st.MemberId)
            .HasColumnType("uuid")
            .HasColumnName("MemberId");

        builder.Property(st => st.CashierId)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("CashierId");

        builder.Property(st => st.TotalAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)")
            .HasColumnName("TotalAmount");

        builder.Property(st => st.DiscountAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m)
            .HasColumnName("DiscountAmount");

        builder.Property(st => st.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("PaymentMethod")
            .HasConversion<string>();

        builder.Property(st => st.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Status")
            .HasConversion<string>();

        builder.Property(st => st.CompletedAt)
            .HasColumnName("CompletedAt")
            .HasColumnType("timestamptz");

        builder.Property(st => st.VoidedAt)
            .HasColumnName("VoidedAt")
            .HasColumnType("timestamptz");

        builder.Property(st => st.IdempotencyKey)
            .HasMaxLength(256)
            .HasColumnName("IdempotencyKey");

        builder.HasIndex(st => new { st.TenantId, st.ShopId, st.Status })
            .HasDatabaseName("IX_sales_transactions_TenantId_ShopId_Status");

        builder.HasIndex(st => st.MemberId)
            .HasDatabaseName("IX_sales_transactions_MemberId");

        // BE-REC-001 R1: partial unique index on (TenantId, ShopId, IdempotencyKey) when not null
        builder.HasIndex(st => new { st.TenantId, st.ShopId, st.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL")
            .HasDatabaseName("UIX_sales_transactions_TenantId_ShopId_IdempotencyKey");

        builder.HasQueryFilter(st => !st.IsDeleted);
    }
}
