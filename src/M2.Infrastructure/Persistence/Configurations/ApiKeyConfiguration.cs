using M2.Domain.ApiKeys;
using M2.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class ApiKeyConfiguration : BaseEntityConfiguration<ApiKey>
{
    public override void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys", "m2");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Scopes).HasConversion(
            v => string.Join(",", v),
            v => v.Split(",", StringSplitOptions.RemoveEmptyEntries));
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamptz");

        builder.HasIndex(x => new { x.TenantId, x.KeyHash })
            .IsUnique()
            .HasDatabaseName("IX_api_keys_TenantId_KeyHash");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
