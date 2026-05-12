using M2.SharedKernel;

namespace M2.Domain.Sales;

public class SalesLineItem : BaseEntity
{
    public Guid TransactionId { get; protected set; }
    /// <summary>SAP product code.</summary>
    public string ProductId { get; protected set; } = string.Empty;
    public string ProductNameEn { get; protected set; } = string.Empty;
    public string ProductNameZht { get; protected set; } = string.Empty;
    public int Quantity { get; protected set; }
    public decimal UnitPrice { get; protected set; }
    public decimal DiscountAmount { get; protected set; }
    public decimal LineTotal { get; protected set; }

    protected SalesLineItem() { }

    public SalesLineItem(
        Guid tenantId,
        Guid shopId,
        Guid transactionId,
        string productId,
        string productNameEn,
        string productNameZht,
        int quantity,
        decimal unitPrice,
        decimal discountAmount = 0)
    {
        TenantId = tenantId;
        ShopId = shopId;
        TransactionId = transactionId;
        ProductId = productId;
        ProductNameEn = productNameEn;
        ProductNameZht = productNameZht;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountAmount = discountAmount;
        LineTotal = (unitPrice * quantity) - discountAmount;
    }
}
