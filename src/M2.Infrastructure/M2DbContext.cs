using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace M2.Infrastructure;

/// <summary>
/// Main EF Core 9 DbContext. Entity configurations added incrementally per epic.
/// PostgreSQL provider (DB-001). TenantId + ShopId applied globally (ADR-013).
/// Default schema: m2 (all tables live under the m2 schema).
/// </summary>
public class M2DbContext(DbContextOptions<M2DbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("m2");

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
