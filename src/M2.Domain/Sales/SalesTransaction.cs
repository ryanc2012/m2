using M2.SharedKernel;

namespace M2.Domain.Sales;

public class SalesTransaction : BaseEntity
{
    /// <summary>Null for guest checkout.</summary>
    public Guid? MemberId { get; protected set; }
    /// <summary>SAP employee code of the cashier.</summary>
    public string CashierId { get; protected set; } = string.Empty;
    public decimal TotalAmount { get; protected set; }
    public decimal DiscountAmount { get; protected set; }
    public PaymentMethod PaymentMethod { get; protected set; }
    public SalesStatus Status { get; protected set; } = SalesStatus.Pending;
    public DateTimeOffset? CompletedAt { get; protected set; }
    public DateTimeOffset? VoidedAt { get; protected set; }

    private readonly List<SalesLineItem> _lineItems = [];
    public IReadOnlyCollection<SalesLineItem> LineItems => _lineItems.AsReadOnly();

    protected SalesTransaction() { }

    public SalesTransaction(
        Guid tenantId,
        Guid shopId,
        Guid? memberId,
        string cashierId,
        PaymentMethod paymentMethod)
    {
        TenantId = tenantId;
        ShopId = shopId;
        MemberId = memberId;
        CashierId = cashierId;
        PaymentMethod = paymentMethod;
        Status = SalesStatus.Pending;
    }

    public void AddLineItem(SalesLineItem item)
    {
        _lineItems.Add(item);
        RecalculateTotals();
    }

    public void Complete()
    {
        Status = SalesStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Void()
    {
        Status = SalesStatus.Voided;
        VoidedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRefunded()
    {
        Status = SalesStatus.Refunded;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void RecalculateTotals()
    {
        TotalAmount = _lineItems.Sum(i => i.LineTotal);
        DiscountAmount = _lineItems.Sum(i => i.DiscountAmount);
    }
}
