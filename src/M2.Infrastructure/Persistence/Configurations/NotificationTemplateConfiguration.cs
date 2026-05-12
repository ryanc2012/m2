using M2.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration : BaseEntityConfiguration<NotificationTemplate>
{
    public override void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(t => t.Type)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("Type")
            .HasConversion<string>();

        builder.OwnsOneBilingual(t => t.Title);
        builder.OwnsOneBilingual(t => t.Body);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
