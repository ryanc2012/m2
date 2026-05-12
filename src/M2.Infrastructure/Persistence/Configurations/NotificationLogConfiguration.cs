using M2.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnType("uuid").ValueGeneratedNever();

        builder.Property(l => l.TemplateId)
            .IsRequired()
            .HasColumnName("TemplateId")
            .HasColumnType("uuid");

        builder.Property(l => l.RecipientUserId)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("RecipientUserId");

        builder.Property(l => l.SentAt)
            .IsRequired()
            .HasColumnName("SentAt")
            .HasColumnType("timestamptz");

        builder.Property(l => l.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Status")
            .HasConversion<string>();

        builder.Property(l => l.ErrorMessage)
            .HasColumnName("ErrorMessage")
            .HasColumnType("text");

        builder.HasIndex(l => l.TemplateId)
            .HasDatabaseName("IX_notification_logs_TemplateId");
    }
}
