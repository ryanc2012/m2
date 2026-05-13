using M2.Domain.Sales;
using M2.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Sales;

internal sealed class SalesService(
    M2DbContext db,
    IIdempotencyContext idempotencyCtx,
    ILogger<SalesService> logger) : ISalesService
{
    public async Task<Result<SalesTransaction>> CreateTransactionAsync(
        Guid tenantId, Guid shopId, Guid? memberId, string cashierId,
        PaymentMethod paymentMethod, IEnumerable<LineItemRequest> lineItems,
        CancellationToken ct = default)
    {
        // BE-REC-001 R1: idempotency — return existing if same key already committed
        var idempotencyKey = idempotencyCtx.Key;
        if (idempotencyKey is not null)
        {
            var existing = await db.SalesTransactions
                .Include(t => t.LineItems)
                .FirstOrDefaultAsync(t =>
                    t.TenantId == tenantId &&
                    t.ShopId == shopId &&
                    t.IdempotencyKey == idempotencyKey, ct);

            if (existing is not null)
            {
                logger.LogInformation(
                    "Idempotent replay: returning existing transaction {Id} for key={Key}",
                    existing.Id, idempotencyKey);
                return Result.Success(existing);
            }
        }

        var transaction = new SalesTransaction(tenantId, shopId, memberId, cashierId, paymentMethod, idempotencyKey);

        foreach (var req in lineItems)
        {
            var item = new SalesLineItem(
                tenantId, shopId, transaction.Id,
                req.ProductId, req.ProductNameEn, req.ProductNameZht,
                req.Quantity, req.UnitPrice, req.DiscountAmount);
            transaction.AddLineItem(item);
        }

        db.SalesTransactions.Add(transaction);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Sales transaction created: id={Id} cashier={CashierId} total={Total} tenant={TenantId}",
            transaction.Id, cashierId, transaction.TotalAmount, tenantId);

        return Result.Success(transaction);
    }

    public async Task<Result<SalesTransaction>> CompleteTransactionAsync(Guid id, CancellationToken ct = default)
    {
        var tx = await db.SalesTransactions.FindAsync([id], ct);
        if (tx is null)
            return Result.Failure<SalesTransaction>($"Transaction {id} not found.");

        if (tx.Status != SalesStatus.Pending)
            return Result.Failure<SalesTransaction>(
                $"Transaction {id} cannot be completed — current status is {tx.Status}.");

        tx.Complete();
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Transaction completed: id={Id}", id);
        return Result.Success(tx);
    }

    public async Task<Result<SalesTransaction>> VoidTransactionAsync(Guid id, CancellationToken ct = default)
    {
        var tx = await db.SalesTransactions.FindAsync([id], ct);
        if (tx is null)
            return Result.Failure<SalesTransaction>($"Transaction {id} not found.");

        if (tx.Status != SalesStatus.Pending)
            return Result.Failure<SalesTransaction>(
                $"Transaction {id} cannot be voided — current status is {tx.Status}.");

        tx.Void();
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Transaction voided: id={Id}", id);
        return Result.Success(tx);
    }

    public async Task<Result<SalesTransaction>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tx = await db.SalesTransactions
            .Include(t => t.LineItems)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return tx is not null
            ? Result.Success(tx)
            : Result.Failure<SalesTransaction>($"Transaction {id} not found.");
    }
}
