using M2.Domain.Approvals;
using M2.Domain.Members;
using M2.Domain.Notifications;
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
    // Members
    public DbSet<Member> Members => Set<Member>();
    public DbSet<OtpRequest> OtpRequests => Set<OtpRequest>();

    // Approvals
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<ApprovalPolicy> ApprovalPolicies => Set<ApprovalPolicy>();

    // Notifications
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("m2");

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
