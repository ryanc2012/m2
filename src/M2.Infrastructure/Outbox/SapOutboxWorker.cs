using M2.Domain.Sap;
using M2.SapConnector;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Text.Json;

namespace M2.Infrastructure.Outbox;

/// <summary>
/// Hangfire recurring job that processes pending SAP outbox entries.
/// Scheduled every 30 seconds in Platform.Api/Program.cs (ADR-006, ADR-017).
/// Uses a Polly exponential-backoff retry pipeline (3 attempts) per entry.
/// </summary>
public sealed class SapOutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SapOutboxWorker> logger)
{
    private static readonly ResiliencePipeline<bool> _retryPipeline =
        new ResiliencePipelineBuilder<bool>()
            .AddRetry(new RetryStrategyOptions<bool>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<bool>().HandleResult(false)
            })
            .Build();

    public async Task ProcessAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<M2DbContext>();
        var sapClient = scope.ServiceProvider.GetRequiredService<ISapODataClient>();

        var pending = await db.SapOutboxEntries
            .Where(e => e.Status == SapOutboxStatus.Pending)
            .OrderBy(e => e.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return;

        logger.LogInformation("[SapOutboxWorker] Processing {Count} pending entries.", pending.Count);

        foreach (var entry in pending)
        {
            ct.ThrowIfCancellationRequested();

            var success = await _retryPipeline.ExecuteAsync(
                async innerCt => await PostEntryAsync(entry, sapClient, innerCt),
                ct);

            if (success)
            {
                entry.MarkSent();
                logger.LogInformation("[SapOutboxWorker] Entry {Id} posted to SAP.", entry.Id);
            }
            else
            {
                logger.LogWarning("[SapOutboxWorker] Entry {Id} failed after retries. Error: {Error}",
                    entry.Id, entry.ErrorMessage);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<bool> PostEntryAsync(
        SapOutboxEntry entry,
        ISapODataClient sapClient,
        CancellationToken ct)
    {
        try
        {
            if (entry.Operation == nameof(ISapODataClient.PostGoodsMovementAsync))
            {
                var payload = JsonSerializer.Deserialize<SapGoodsMovementPayload>(entry.Payload);
                if (payload is null)
                {
                    entry.MarkFailed("Payload deserialization returned null.");
                    return false;
                }

                var result = await sapClient.PostGoodsMovementAsync(entry.TenantId, payload, ct);

                if (result.IsSuccess)
                    return true;

                entry.MarkFailed(result.Error ?? "Unknown SAP error.");
                return false;
            }

            logger.LogWarning("[SapOutboxWorker] Unknown operation '{Op}' on entry {Id} — skipping.",
                entry.Operation, entry.Id);
            entry.MarkFailed($"Unknown operation: {entry.Operation}");
            return false;
        }
        catch (Exception ex)
        {
            entry.MarkFailed(ex.Message);
            logger.LogError(ex, "[SapOutboxWorker] Exception processing entry {Id}.", entry.Id);
            return false;
        }
    }
}
