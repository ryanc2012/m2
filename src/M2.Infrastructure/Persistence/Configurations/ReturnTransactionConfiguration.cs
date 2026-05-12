using M2.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class ReturnTransactionConfiguration : BaseEntityConfiguration<ReturnTransaction>
{
    public override void Configure(EntityTypeBuilder<ReturnTransaction> builder)
    {
        builder.ToTable("return_transactions");
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(rt => rt.OriginalTransactionId)
            .IsRequired()
            .HasColumnType("uuid")
            .HasColumnName("OriginalTransactionId");

        builder.Property(rt => rt.Reason)
            .IsRequired()
            .HasColumnType("text")
            .HasColumnName("Reason");

        builder.Property(rt => rt.RefundAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)")
            .HasColumnName("RefundAmount");

        builder.Property(rt => rt.RefundMethod)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("RefundMethod")
            .HasConversion<string>();

        builder.Property(rt => rt.ProcessedAt)
            .HasColumnName("ProcessedAt")
            .HasColumnType("timestamptz");

        builder.Property(rt => rt.IsComplete)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("IsComplete");

        builder.HasOne<SalesTransaction>()
            .WithMany()
            .HasForeignKey(rt => rt.OriginalTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(rt => !rt.IsDeleted);
    }
}
