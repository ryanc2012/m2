using M2.SharedKernel;

namespace M2.Domain.GoodsReceipt;

public class GoodsReceiptNote : BaseEntity
{
    public string SapDeliveryNoteNumber { get; protected set; } = string.Empty;
    public GoodsReceiptStatus Status { get; protected set; } = GoodsReceiptStatus.Pending;
    public DateTimeOffset? ReceivedAt { get; protected set; }
    public DateTimeOffset? ConfirmedAt { get; protected set; }

    private readonly List<GoodsReceiptLineItem> _lineItems = [];
    public IReadOnlyCollection<GoodsReceiptLineItem> LineItems => _lineItems.AsReadOnly();
    public string? DiscrepancyNote { get; protected set; }

    protected GoodsReceiptNote() { }

    public GoodsReceiptNote(
        Guid tenantId,
        Guid shopId,
        string sapDeliveryNoteNumber)
    {
        TenantId = tenantId;
        ShopId = shopId;
        SapDeliveryNoteNumber = sapDeliveryNoteNumber;
        Status = GoodsReceiptStatus.Pending;
    }

    public void MarkReceived()
    {
        ReceivedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Confirm()
    {
        if (Status == GoodsReceiptStatus.Confirmed)
            throw new InvalidOperationException("Goods receipt note is already confirmed.");

        Status = GoodsReceiptStatus.Confirmed;
        ConfirmedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDiscrepancy()
    {
        Status = GoodsReceiptStatus.Discrepancy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Records a note-level discrepancy with a mandatory explanatory note.</summary>
    public void RecordDiscrepancy(string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Discrepancy note must not be empty.", nameof(note));

        Status = GoodsReceiptStatus.Discrepancy;
        DiscrepancyNote = note;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddLineItem(GoodsReceiptLineItem item) => _lineItems.Add(item);
}
