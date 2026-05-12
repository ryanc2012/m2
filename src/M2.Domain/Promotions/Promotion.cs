using M2.SharedKernel;

namespace M2.Domain.Promotions;

public class Promotion : BaseEntity
{
    public BilingualText Name { get; protected set; } = BilingualText.Empty;
    public PromotionType Type { get; protected set; }
    public PromotionStatus Status { get; protected set; } = PromotionStatus.Draft;
    /// <summary>Stores discount formula as JSON (e.g. percentage, fixed amount, buy-X-get-Y params).</summary>
    public string FormulaJson { get; protected set; } = string.Empty;
    public DateTimeOffset StartDate { get; protected set; }
    public DateTimeOffset EndDate { get; protected set; }
    /// <summary>Controls whether this promotion can stack with others (ADR-020).</summary>
    public bool IsStackable { get; protected set; }
    /// <summary>Links to approval engine request when promotion requires approval.</summary>
    public Guid? ApprovalRequestId { get; protected set; }

    private readonly List<PromotionProduct> _products = [];
    public IReadOnlyCollection<PromotionProduct> Products => _products.AsReadOnly();

    private readonly List<Coupon> _coupons = [];
    public IReadOnlyCollection<Coupon> Coupons => _coupons.AsReadOnly();

    protected Promotion() { }

    public Promotion(
        Guid tenantId,
        Guid shopId,
        BilingualText name,
        PromotionType type,
        string formulaJson,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        bool isStackable)
    {
        TenantId = tenantId;
        ShopId = shopId;
        Name = name;
        Type = type;
        FormulaJson = formulaJson;
        StartDate = startDate;
        EndDate = endDate;
        IsStackable = isStackable;
        Status = PromotionStatus.Draft;
    }

    public void SubmitForApproval(Guid approvalRequestId)
    {
        Status = PromotionStatus.PendingApproval;
        ApprovalRequestId = approvalRequestId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (Status == PromotionStatus.Active)
            throw new InvalidOperationException("Promotion is already active.");
        Status = PromotionStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Pause()
    {
        Status = PromotionStatus.Paused;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Expire()
    {
        Status = PromotionStatus.Expired;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
