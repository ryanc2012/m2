using M2.Domain.Sales;
using M2.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Sales;

internal sealed class ReturnService(M2DbContext db, ISalesService salesService, ILogger<ReturnService> logger) : IReturnService
{
    /// <summary>
    /// Initiates return, enforcing that the refund method equals the original payment method (ADR-016)
    /// and that the refund amount does not exceed the original transaction total.
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

        if (refundAmount > original.TotalAmount)
            return Result.Failure<ReturnTransaction>(
                $"Refund amount {refundAmount} exceeds original transaction total {original.TotalAmount}.");

        // ADR-016: refund must use the same payment method as the original transaction
        var returnTx = new ReturnTransaction(
            tenantId, shopId, originalTransactionId, reason, refundAmount, original.PaymentMethod);

        db.ReturnTransactions.Add(returnTx);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Return initiated: id={Id} originalTx={OriginalId} refundMethod={Method} amount={Amount}",
            returnTx.Id, originalTransactionId, original.PaymentMethod, refundAmount);

        return Result.Success(returnTx);
    }

    public async Task<Result<ReturnTransaction>> CompleteReturnAsync(Guid id, CancellationToken ct = default)
    {
        var returnTx = await db.ReturnTransactions.FindAsync([id], ct);
        if (returnTx is null)
            return Result.Failure<ReturnTransaction>($"Return {id} not found.");

        if (returnTx.IsComplete)
            return Result.Failure<ReturnTransaction>($"Return {id} is already complete.");

        returnTx.Complete();
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Return completed: id={Id}", id);
        return Result.Success(returnTx);
    }
}
