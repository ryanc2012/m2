using M2.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

/// <summary>
/// Base EF Core configuration applied to every entity that extends <see cref="BaseEntity"/>.
/// Encodes the mandatory multi-tenancy (TenantId), multi-store (ShopId, ADR-013),
/// audit, and soft-delete columns shared across all tables.
/// </summary>
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // Multi-tenancy — application layer is responsible for always setting this.
        builder.Property(e => e.TenantId)
            .IsRequired()
            .HasColumnName("TenantId");

        // Multi-store: first-class on every entity from Day 1 (ADR-013).
        builder.Property(e => e.ShopId)
            .IsRequired()
            .HasColumnName("ShopId");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnName("CreatedAt")
            .HasColumnType("timestamptz");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(256)
            .HasColumnName("CreatedBy");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(256)
            .HasColumnName("UpdatedBy");

        // Soft delete — concrete configurations should add a global query filter:
        //   builder.HasQueryFilter(e => !e.IsDeleted);
        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("IsDeleted");

        builder.Property(e => e.DeletedAt)
            .HasColumnName("DeletedAt")
            .HasColumnType("timestamptz");

        builder.Property(e => e.DeletedBy)
            .HasMaxLength(256)
            .HasColumnName("DeletedBy");
    }
}
