using M2.Domain.Sales;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Sales;

/// <summary>In-memory stub ReturnService. EF wiring deferred to Sprint 4.</summary>
internal sealed class ReturnService(ISalesService salesService, ILogger<ReturnService> logger) : IReturnService
{
    private static readonly Dictionary<Guid, ReturnTransaction> _returns = [];

    /// <summary>
    /// Initiates return, enforcing that the refund method equals the original payment method (ADR-016).
    /// </summary>
    public async Task<Result<ReturnTransaction>> InitiateReturnAsync(
        Guid tenantId, Guid shopId, Guid originalTransactionId,
        string reason, decimal refundAmount, CancellationToken ct = default)
    {
        var txResult = await salesService.GetByIdAsync(originalTransactionId, ct);
        if (!txResult.IsSuccess)
            return Result.Failure<ReturnTransaction>(txResult.Error!);

        var original = txResult.Value!;

        if (original.Status != SalesStatus.Completed)
            return Result.Failure<ReturnTransaction>(
                $"Transaction {originalTransactionId} is not completed; cannot return.");

        // ADR-016: refund must use the same payment method as the original transaction
        var refundMethod = original.PaymentMethod;

        var returnTx = new ReturnTransaction(
            tenantId, shopId, originalTransactionId, reason, refundAmount, refundMethod);
        _returns[returnTx.Id] = returnTx;

        logger.LogInformation(
            "Return initiated: id={Id} originalTx={OriginalId} refundMethod={Method} amount={Amount}",
            returnTx.Id, originalTransactionId, refundMethod, refundAmount);

        return Result.Success(returnTx);
    }

    public Task<Result<ReturnTransaction>> CompleteReturnAsync(Guid id, CancellationToken ct = default)
    {
        if (!_returns.TryGetValue(id, out var returnTx))
            return Task.FromResult(Result.Failure<ReturnTransaction>($"Return {id} not found."));

        if (returnTx.IsComplete)
            return Task.FromResult(Result.Failure<ReturnTransaction>($"Return {id} is already complete."));

        returnTx.Complete();
        logger.LogInformation("Return completed: id={Id}", id);
        return Task.FromResult(Result.Success(returnTx));
    }
}
