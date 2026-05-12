namespace M2.Domain.Promotions;

/// <summary>Join entity linking a promotion to the SAP product codes it applies to.</summary>
public class PromotionProduct
{
    public Guid PromotionId { get; protected set; }
    /// <summary>SAP product code.</summary>
    public string ProductId { get; protected set; } = string.Empty;
    public decimal DiscountValue { get; protected set; }

    protected PromotionProduct() { }

    public PromotionProduct(Guid promotionId, string productId, decimal discountValue)
    {
        PromotionId = promotionId;
        ProductId = productId;
        DiscountValue = discountValue;
    }
}
