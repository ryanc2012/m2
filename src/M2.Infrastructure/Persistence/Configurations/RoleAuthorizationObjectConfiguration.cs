using M2.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class RoleAuthorizationObjectConfiguration : BaseEntityConfiguration<RoleAuthorizationObject>
{
    public override void Configure(EntityTypeBuilder<RoleAuthorizationObject> builder)
    {
        builder.ToTable("role_authorization_objects");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(o => o.AuthorizationRoleId)
            .IsRequired()
            .HasColumnName("AuthorizationRoleId");

        builder.Property(o => o.AuthorizationObject)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("AuthorizationObject");

        builder.HasIndex(o => new { o.AuthorizationRoleId, o.AuthorizationObject })
            .IsUnique()
            .HasDatabaseName("IX_role_authorization_objects_RoleId_Object");

        builder.HasIndex(o => o.AuthorizationRoleId)
            .HasDatabaseName("IX_role_authorization_objects_AuthorizationRoleId");

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}
