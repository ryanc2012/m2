namespace M2.Infrastructure;

/// <summary>No-op outbox — replaced when Hangfire is wired in Sprint 4.</summary>
internal sealed class NoOpOutboxService : IOutboxService
{
    public Task EnqueueAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class => Task.CompletedTask;

    public Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}
