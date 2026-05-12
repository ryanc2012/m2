using M2.Domain.Approvals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class ApprovalRequestConfiguration : BaseEntityConfiguration<ApprovalRequest>
{
    public override void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("approval_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(r => r.EntityType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("EntityType");

        builder.Property(r => r.EntityId)
            .IsRequired()
            .HasColumnName("EntityId")
            .HasColumnType("uuid");

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Status")
            .HasConversion<string>();

        builder.Property(r => r.CurrentStep)
            .IsRequired()
            .HasColumnName("CurrentStep");

        builder.HasMany(r => r.Steps)
            .WithOne()
            .HasForeignKey(s => s.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
