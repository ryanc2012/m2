using M2.Domain.Sap;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class SapOutboxEntryConfiguration : BaseEntityConfiguration<SapOutboxEntry>
{
    public override void Configure(EntityTypeBuilder<SapOutboxEntry> builder)
    {
        builder.ToTable("sap_outbox_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(e => e.Operation)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("Operation");

        builder.Property(e => e.Payload)
            .IsRequired()
            .HasColumnType("text")
            .HasColumnName("Payload");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Status")
            .HasConversion<string>();

        builder.Property(e => e.ProcessedAt)
            .HasColumnName("ProcessedAt")
            .HasColumnType("timestamptz");

        builder.Property(e => e.RetryCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnName("RetryCount");

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000)
            .HasColumnName("ErrorMessage");

        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_sap_outbox_entries_TenantId_Status");

        builder.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("IX_sap_outbox_entries_Status_CreatedAt");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
