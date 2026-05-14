using M2.Domain.Promotions;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace M2.Infrastructure.Promotions;

/// <summary>
/// Discount engine with real formula evaluation (S4.3).
/// Applies active stackable promotions per ADR-020.
/// Formula parameters are parsed from Promotion.FormulaJson.
/// </summary>
internal sealed class DiscountEngine(
    IPromotionService promotionService,
    ILogger<DiscountEngine> logger) : IDiscountEngine
{
    public async Task<Result<DiscountResult>> CalculateAsync(
        IEnumerable<CartItem> cartItems,
        Guid? memberId,
        Guid shopId,
        CancellationToken ct = default)
    {
        var items = cartItems.ToList();
        var originalTotal = items.Sum(i => i.UnitPrice * i.Quantity);

        var activeResult = await promotionService.GetActiveForShopAsync(WellKnownTenants.Default, shopId, ct);
        if (!activeResult.IsSuccess)
            return Result<DiscountResult>.Failure(activeResult.Error!);

        var applicablePromotions = activeResult.Value!
            .Where(p => p.IsStackable)
            .ToList();

        var discountAmount = applicablePromotions
            .Sum(p => p.Type switch
            {
                PromotionType.PercentDiscount =>
                    originalTotal * (ExtractPercentage(p.FormulaJson) / 100m),
                PromotionType.FixedDiscount =>
                    Math.Min(ExtractFixedAmount(p.FormulaJson), originalTotal),
                PromotionType.BuyXGetY =>
                    CalculateBuyXGetYDiscount(items, p),
                _ => 0m
            });

        // Cap total discount at the original total
        discountAmount = Math.Min(discountAmount, originalTotal);

        logger.LogInformation(
            "Discount calculated: shopId={ShopId} originalTotal={Original} discount={Discount} promotionsConsidered={Count}",
            shopId, originalTotal, discountAmount, applicablePromotions.Count);

        var result = new DiscountResult(
            OriginalTotal: originalTotal,
            DiscountAmount: discountAmount,
            FinalTotal: originalTotal - discountAmount,
            AppliedPromotionIds: applicablePromotions.Select(p => p.Id.ToString()).ToList().AsReadOnly());

        return Result.Success(result);
    }

    private static decimal ExtractPercentage(string formulaJson)
    {
        try
        {
            var doc = JsonDocument.Parse(formulaJson);
            return doc.RootElement.TryGetProperty("percentage", out var el) ? el.GetDecimal() : 0m;
        }
        catch { return 0m; }
    }

    private static decimal ExtractFixedAmount(string formulaJson)
    {
        try
        {
            var doc = JsonDocument.Parse(formulaJson);
            return doc.RootElement.TryGetProperty("amount", out var el) ? el.GetDecimal() : 0m;
        }
        catch { return 0m; }
    }

    private static decimal CalculateBuyXGetYDiscount(List<CartItem> items, Promotion promotion)
    {
        try
        {
            var doc = JsonDocument.Parse(promotion.FormulaJson);
            var buyQty = doc.RootElement.TryGetProperty("buyQty", out var bEl) ? bEl.GetInt32() : 2;
            var getQty = doc.RootElement.TryGetProperty("getQty", out var gEl) ? gEl.GetInt32() : 1;

            var totalQty = items.Sum(i => i.Quantity);
            var sets = totalQty / (buyQty + getQty);
            if (sets <= 0) return 0m;

            // Free items are the cheapest units in the cart
            var cheapestUnitPrice = items.Min(i => i.UnitPrice);
            return sets * getQty * cheapestUnitPrice;
        }
        catch { return 0m; }
    }
}

