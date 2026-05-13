using M2.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class UserRoleAssignmentConfiguration : BaseEntityConfiguration<UserRoleAssignment>
{
    public override void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ToTable("user_role_assignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("UserId");

        builder.Property(a => a.AuthorizationRoleId)
            .IsRequired()
            .HasColumnName("AuthorizationRoleId");

        builder.Property(a => a.ValidFrom)
            .IsRequired()
            .HasColumnName("ValidFrom")
            .HasColumnType("timestamptz");

        builder.Property(a => a.ValidTo)
            .HasColumnName("ValidTo")
            .HasColumnType("timestamptz");

        builder.HasIndex(a => new { a.TenantId, a.UserId })
            .HasDatabaseName("IX_user_role_assignments_TenantId_UserId");

        builder.HasIndex(a => new { a.UserId, a.AuthorizationRoleId })
            .HasDatabaseName("IX_user_role_assignments_UserId_RoleId");

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
