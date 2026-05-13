using M2.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class AuthorizationRoleConfiguration : BaseEntityConfiguration<AuthorizationRole>
{
    public override void Configure(EntityTypeBuilder<AuthorizationRole> builder)
    {
        builder.ToTable("authorization_roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("Name");

        builder.Property(r => r.Description)
            .HasMaxLength(500)
            .HasColumnName("Description");

        builder.HasIndex(r => new { r.TenantId, r.Name })
            .IsUnique()
            .HasDatabaseName("IX_authorization_roles_TenantId_Name");

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
