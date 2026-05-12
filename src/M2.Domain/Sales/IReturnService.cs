using M2.SharedKernel;

namespace M2.Domain.Sales;

public interface IReturnService
{
    /// <summary>
    /// Initiates a return, validating that refund method matches the original payment (ADR-016).
    /// </summary>
    Task<Result<ReturnTransaction>> InitiateReturnAsync(
        Guid tenantId,
        Guid shopId,
        Guid originalTransactionId,
        string reason,
        decimal refundAmount,
        CancellationToken ct = default);

    Task<Result<ReturnTransaction>> CompleteReturnAsync(Guid id, CancellationToken ct = default);
}
