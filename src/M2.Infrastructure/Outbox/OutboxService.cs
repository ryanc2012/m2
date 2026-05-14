using M2.Domain.Sap;
using M2.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace M2.Infrastructure.Outbox;

/// <summary>
/// EF-backed outbox service. Persists SAP messages to the sap_outbox_entries table.
/// Processing is handled by <see cref="SapOutboxWorker"/> (Hangfire recurring job).
/// </summary>
internal sealed class OutboxService(
    M2DbContext context,
    ILogger<OutboxService> logger) : IOutboxService
{
    public async Task EnqueueAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var operation = typeof(TMessage).Name;
        var payload = JsonSerializer.Serialize(message);

        var tenantId = message is ITenantedOutboxMessage tm ? tm.TenantId : WellKnownTenants.Default;
        var shopId = message is IShopScopedOutboxMessage sm ? sm.ShopId : Guid.Empty;

        var entry = new SapOutboxEntry(tenantId, shopId, operation, payload);
        context.SapOutboxEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[OutboxService] Enqueued {Operation} entry {Id} for tenant {TenantId}.",
            operation, entry.Id, tenantId);
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        return await context.SapOutboxEntries
            .CountAsync(e => e.Status == SapOutboxStatus.Pending, cancellationToken);
    }
}

/// <summary>Outbox messages implementing this carry tenant context for proper routing.</summary>
public interface ITenantedOutboxMessage
{
    Guid TenantId { get; }
}

/// <summary>Outbox messages implementing this carry shop context for proper routing.</summary>
public interface IShopScopedOutboxMessage
{
    Guid ShopId { get; }
}
