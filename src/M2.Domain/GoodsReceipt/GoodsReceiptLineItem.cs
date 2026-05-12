using M2.SharedKernel;

namespace M2.Domain.GoodsReceipt;

public class GoodsReceiptLineItem : BaseEntity
{
    public Guid GoodsReceiptNoteId { get; protected set; }
    public string ProductCode { get; protected set; } = string.Empty;
    public BilingualText ProductName { get; protected set; } = BilingualText.Empty;
    public decimal ExpectedQty { get; protected set; }
    public decimal ReceivedQty { get; protected set; }
    public string UnitOfMeasure { get; protected set; } = string.Empty;
    public string? DiscrepancyNote { get; protected set; }

    protected GoodsReceiptLineItem() { }

    public GoodsReceiptLineItem(
        Guid tenantId,
        Guid shopId,
        Guid goodsReceiptNoteId,
        string productCode,
        BilingualText productName,
        decimal expectedQty,
        decimal receivedQty,
        string unitOfMeasure,
        string? discrepancyNote = null)
    {
        TenantId = tenantId;
        ShopId = shopId;
        GoodsReceiptNoteId = goodsReceiptNoteId;
        ProductCode = productCode;
        ProductName = productName;
        ExpectedQty = expectedQty;
        ReceivedQty = receivedQty;
        UnitOfMeasure = unitOfMeasure;
        DiscrepancyNote = discrepancyNote;
    }

    public void RecordReceipt(decimal receivedQty, string? discrepancyNote = null)
    {
        ReceivedQty = receivedQty;
        DiscrepancyNote = discrepancyNote;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordDiscrepancy(decimal receivedQty, string note)
    {
        ReceivedQty = receivedQty;
        DiscrepancyNote = note;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
