using M2.Domain.Sales;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Sales;

/// <summary>In-memory stub SalesService. EF wiring deferred to Sprint 4.</summary>
internal sealed class SalesService(ILogger<SalesService> logger) : ISalesService
{
    private static readonly Dictionary<Guid, SalesTransaction> _transactions = [];

    public Task<Result<SalesTransaction>> CreateTransactionAsync(
        Guid tenantId, Guid shopId, Guid? memberId, string cashierId,
        PaymentMethod paymentMethod, IEnumerable<LineItemRequest> lineItems,
        CancellationToken ct = default)
    {
        var transaction = new SalesTransaction(tenantId, shopId, memberId, cashierId, paymentMethod);

        foreach (var req in lineItems)
        {
            var item = new SalesLineItem(
                tenantId, shopId, transaction.Id,
                req.ProductId, req.ProductNameEn, req.ProductNameZht,
                req.Quantity, req.UnitPrice, req.DiscountAmount);
            transaction.AddLineItem(item);
        }

        _transactions[transaction.Id] = transaction;

        logger.LogInformation(
            "Sales transaction created: id={Id} cashier={CashierId} total={Total} tenant={TenantId}",
            transaction.Id, cashierId, transaction.TotalAmount, tenantId);

        return Task.FromResult(Result.Success(transaction));
    }

    public Task<Result<SalesTransaction>> CompleteTransactionAsync(Guid id, CancellationToken ct = default)
    {
        if (!_transactions.TryGetValue(id, out var tx))
            return Task.FromResult(Result.Failure<SalesTransaction>($"Transaction {id} not found."));

        if (tx.Status != SalesStatus.Pending)
            return Task.FromResult(Result.Failure<SalesTransaction>(
                $"Transaction {id} cannot be completed — current status is {tx.Status}."));

        tx.Complete();
        logger.LogInformation("Transaction completed: id={Id}", id);
        return Task.FromResult(Result.Success(tx));
    }

    public Task<Result<SalesTransaction>> VoidTransactionAsync(Guid id, CancellationToken ct = default)
    {
        if (!_transactions.TryGetValue(id, out var tx))
            return Task.FromResult(Result.Failure<SalesTransaction>($"Transaction {id} not found."));

        if (tx.Status != SalesStatus.Pending)
            return Task.FromResult(Result.Failure<SalesTransaction>(
                $"Transaction {id} cannot be voided — current status is {tx.Status}."));

        tx.Void();
        logger.LogInformation("Transaction voided: id={Id}", id);
        return Task.FromResult(Result.Success(tx));
    }

    public Task<Result<SalesTransaction>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _transactions.TryGetValue(id, out var tx)
            ? Task.FromResult(Result.Success(tx))
            : Task.FromResult(Result.Failure<SalesTransaction>($"Transaction {id} not found."));
    }
}
