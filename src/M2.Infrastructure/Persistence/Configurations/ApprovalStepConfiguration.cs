using M2.Domain.Approvals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class ApprovalStepConfiguration : BaseEntityConfiguration<ApprovalStep>
{
    public override void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("approval_steps");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(s => s.RequestId)
            .IsRequired()
            .HasColumnName("RequestId")
            .HasColumnType("uuid");

        builder.Property(s => s.StepNumber)
            .IsRequired()
            .HasColumnName("StepNumber");

        builder.Property(s => s.ApproverId)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("ApproverId");

        builder.Property(s => s.ApproverType)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("ApproverType")
            .HasConversion<string>();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Status")
            .HasConversion<string>();

        builder.Property(s => s.Comment)
            .HasColumnName("Comment")
            .HasColumnType("text");

        builder.Property(s => s.ActedAt)
            .HasColumnName("ActedAt")
            .HasColumnType("timestamptz");

        builder.HasIndex(s => s.RequestId)
            .HasDatabaseName("IX_approval_steps_RequestId");

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
