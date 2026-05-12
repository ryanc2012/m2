namespace M2.Infrastructure;

/// <summary>
/// Outbox pattern interface (ADR-006 / ADR-017).
/// Reliable SAP write delivery — Hangfire worker retries every 30 seconds.
/// </summary>
public interface IOutboxService
{
    Task EnqueueAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);
}
