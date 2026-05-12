using M2.SharedKernel;

namespace M2.Domain.Sales;

public class ReturnTransaction : BaseEntity
{
    public Guid OriginalTransactionId { get; protected set; }
    public string Reason { get; protected set; } = string.Empty;
    public decimal RefundAmount { get; protected set; }
    /// <summary>Must match the original transaction's payment method (ADR-016).</summary>
    public PaymentMethod RefundMethod { get; protected set; }
    public DateTimeOffset? ProcessedAt { get; protected set; }
    public bool IsComplete { get; protected set; }

    protected ReturnTransaction() { }

    public ReturnTransaction(
        Guid tenantId,
        Guid shopId,
        Guid originalTransactionId,
        string reason,
        decimal refundAmount,
        PaymentMethod refundMethod)
    {
        TenantId = tenantId;
        ShopId = shopId;
        OriginalTransactionId = originalTransactionId;
        Reason = reason;
        RefundAmount = refundAmount;
        RefundMethod = refundMethod;
        IsComplete = false;
    }

    public void Complete()
    {
        IsComplete = true;
        ProcessedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
