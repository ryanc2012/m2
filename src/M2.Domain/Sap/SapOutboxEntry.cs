using M2.SharedKernel;

namespace M2.Domain.Sap;

public class SapOutboxEntry : BaseEntity
{
    public string Operation { get; protected set; } = string.Empty;
    public string Payload { get; protected set; } = string.Empty;
    public SapOutboxStatus Status { get; protected set; } = SapOutboxStatus.Pending;
    public DateTimeOffset? ProcessedAt { get; protected set; }
    public int RetryCount { get; protected set; }
    public string? ErrorMessage { get; protected set; }

    protected SapOutboxEntry() { }

    public SapOutboxEntry(
        Guid tenantId,
        Guid shopId,
        string operation,
        string payload)
    {
        TenantId = tenantId;
        ShopId = shopId;
        Operation = operation;
        Payload = payload;
        Status = SapOutboxStatus.Pending;
        RetryCount = 0;
    }

    public void MarkSent()
    {
        Status = SapOutboxStatus.Sent;
        ProcessedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = SapOutboxStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ResetToPending()
    {
        Status = SapOutboxStatus.Pending;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
