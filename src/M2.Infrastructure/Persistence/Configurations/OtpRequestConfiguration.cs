using M2.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class OtpRequestConfiguration : IEntityTypeConfiguration<OtpRequest>
{
    public void Configure(EntityTypeBuilder<OtpRequest> builder)
    {
        builder.ToTable("otp_requests");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnType("uuid").ValueGeneratedNever();

        builder.Property(o => o.MemberId)
            .IsRequired()
            .HasColumnName("MemberId")
            .HasColumnType("uuid");

        builder.Property(o => o.Code)
            .IsRequired()
            .HasMaxLength(6)
            .HasColumnName("Code");

        builder.Property(o => o.ExpiresAt)
            .IsRequired()
            .HasColumnName("ExpiresAt")
            .HasColumnType("timestamptz");

        builder.Property(o => o.IsUsed)
            .IsRequired()
            .HasColumnName("IsUsed");

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasColumnName("CreatedAt")
            .HasColumnType("timestamptz");

        builder.HasOne<M2.Domain.Members.Member>()
            .WithMany(m => m.OtpRequests)
            .HasForeignKey(o => o.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.MemberId, o.IsUsed })
            .HasDatabaseName("IX_otp_requests_MemberId_IsUsed");
    }
}
