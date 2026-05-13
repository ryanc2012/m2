using M2.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class ObjectFieldValueConfiguration : BaseEntityConfiguration<ObjectFieldValue>
{
    public override void Configure(EntityTypeBuilder<ObjectFieldValue> builder)
    {
        builder.ToTable("object_field_values");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(v => v.RoleAuthorizationObjectId)
            .IsRequired()
            .HasColumnName("RoleAuthorizationObjectId");

        builder.Property(v => v.FieldName)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("FieldName");

        builder.Property(v => v.FieldValue)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("FieldValue");

        builder.HasIndex(v => v.RoleAuthorizationObjectId)
            .HasDatabaseName("IX_object_field_values_RoleAuthorizationObjectId");

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
