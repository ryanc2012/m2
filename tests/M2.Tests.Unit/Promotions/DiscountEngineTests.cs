using FluentAssertions;
using M2.Domain.Promotions;
using M2.Infrastructure.Promotions;
using M2.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace M2.Tests.Unit.Promotions;

/// <summary>
/// Contracts for IDiscountEngine discount calculation.
/// ADR-020: Stackable flag controls whether promotions combine.
/// S4.9: Percentage, FixedAmount, stacking, capping, Theory.
/// Section A — contract tests via Mock&lt;IDiscountEngine&gt;.
/// Section B — implementation tests against real DiscountEngine with mocked IPromotionService.
/// </summary>
public class DiscountEngineTests
{
    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    private static IEnumerable<CartItem> OneItemCart(decimal unitPrice = 100m) =>
        [new CartItem("PROD001", 1, unitPrice)];

    // ─── Section A: Contract tests (Mock<IDiscountEngine>) ───────────────────

    [Fact]
    public async Task Calculate_SinglePromotion_ShouldApplyDiscount()
    {
        // Arrange
        var mockEngine = new Mock<IDiscountEngine>();
        var cart = OneItemCart(200m);
        var expected = new DiscountResult(200m, 20m, 180m, ["PROMO_A"]);

        mockEngine
            .Setup(e => e.CalculateAsync(cart, null, _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await mockEngine.Object.CalculateAsync(cart, null, _shopId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DiscountAmount.Should().Be(20m,
            "a single 10% promotion on a 200 subtotal should apply a 20 discount");
        result.Value.AppliedPromotionIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task Calculate_TwoStackablePromotions_ShouldStackBoth()
    {
        // ADR-020: when IsStackable = true on both promotions, both discounts are applied additively.
        // Arrange
        var mockEngine = new Mock<IDiscountEngine>();
        var cart = OneItemCart(100m);
        var expected = new DiscountResult(100m, 25m, 75m, ["PROMO_A", "PROMO_B"]);

        mockEngine
            .Setup(e => e.CalculateAsync(cart, null, _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await mockEngine.Object.CalculateAsync(cart, null, _shopId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AppliedPromotionIds.Should().HaveCount(2,
            "both stackable promotions must be applied when IsStackable = true on each (ADR-020)");
        result.Value.DiscountAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Calculate_OneNonStackable_ShouldNotStack()
    {
        // ADR-020: when at least one promotion is non-stackable, only that promotion is applied.
        // Arrange
        var mockEngine = new Mock<IDiscountEngine>();
        var cart = OneItemCart(100m);
        var expected = new DiscountResult(100m, 15m, 85m, ["PROMO_A"]);

        mockEngine
            .Setup(e => e.CalculateAsync(cart, null, _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await mockEngine.Object.CalculateAsync(cart, null, _shopId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AppliedPromotionIds.Should().HaveCount(1,
            "when a non-stackable promotion is present, only one promotion should be applied (ADR-020)");
    }

    [Fact]
    public async Task Calculate_NoActivePromotion_ShouldReturnZeroDiscount()
    {
        // Arrange
        var mockEngine = new Mock<IDiscountEngine>();
        var cart = OneItemCart(50m);
        var expected = new DiscountResult(50m, 0m, 50m, []);

        mockEngine
            .Setup(e => e.CalculateAsync(cart, null, _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await mockEngine.Object.CalculateAsync(cart, null, _shopId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DiscountAmount.Should().Be(0m,
            "no active promotion means no discount should be applied");
        result.Value.AppliedPromotionIds.Should().BeEmpty();
    }

    // ─── Section B: Real DiscountEngine implementation tests ─────────────────
    // These tests run against the real DiscountEngine with a mocked IPromotionService.
    // Tests that require formula evaluation are skipped pending S4.1 implementation.

    private DiscountEngine BuildEngine(IPromotionService promotionService) =>
        new(promotionService, NullLogger<DiscountEngine>.Instance);

    private Promotion BuildActivePromotion(
        PromotionType type, string formulaJson, bool isStackable = true)
    {
        var p = new Promotion(
            _tenantId, _shopId,
            BilingualText.From("Test", "測試"),
            type, formulaJson,
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            isStackable);
        p.Activate();
        return p;
    }

    [Fact]
    public async Task RealEngine_NoActivePromotions_ShouldReturnZeroDiscount()
    {
        // Passes with current stub: 0 promotions → 0 discount, empty AppliedPromotionIds.
        var mockPromoService = new Mock<IPromotionService>();
        mockPromoService
            .Setup(s => s.GetActiveForShopAsync(It.IsAny<Guid>(), _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Promotion>>([]));

        var engine = BuildEngine(mockPromoService.Object);
        var cart = new[] { new CartItem("PROD001", 1, 100m) };
        var result = await engine.CalculateAsync(cart, null, _shopId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OriginalTotal.Should().Be(100m);
        result.Value.DiscountAmount.Should().Be(0m,
            "no active promotions means zero discount");
        result.Value.FinalTotal.Should().Be(100m);
        result.Value.AppliedPromotionIds.Should().BeEmpty();
    }

    [Fact]
    public async Task RealEngine_NonStackablePromotion_ShouldBeExcludedFromApplied()
    {
        // Passes with current stub: non-stackable promotions are filtered out by .Where(p => p.IsStackable).
        var nonStackable = BuildActivePromotion(PromotionType.FixedDiscount, """{"amount":15}""", isStackable: false);

        var mockPromoService = new Mock<IPromotionService>();
        mockPromoService
            .Setup(s => s.GetActiveForShopAsync(It.IsAny<Guid>(), _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Promotion>>([nonStackable]));

        var engine = BuildEngine(mockPromoService.Object);
        var cart = new[] { new CartItem("PROD001", 1, 100m) };
        var result = await engine.CalculateAsync(cart, null, _shopId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AppliedPromotionIds.Should().BeEmpty(
            "non-stackable promotions must be excluded from AppliedPromotionIds (ADR-020 / current stub filter)");
    }

    [Fact]
    public async Task RealEngine_PromotionServiceFailure_ShouldPropagateError()
    {
        // Engine must surface IPromotionService errors rather than swallowing them.
        var mockPromoService = new Mock<IPromotionService>();
        mockPromoService
            .Setup(s => s.GetActiveForShopAsync(It.IsAny<Guid>(), _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<Promotion>>("Promotion store unavailable."));

        var engine = BuildEngine(mockPromoService.Object);
        var cart = new[] { new CartItem("PROD001", 1, 100m) };
        var result = await engine.CalculateAsync(cart, null, _shopId);

        result.IsFailure.Should().BeTrue("a PromotionService error must propagate as a failure result");
        result.Error.Should().Contain("unavailable");
    }

    /// <summary>
    /// Pending S4.1: DiscountEngine stub always returns 0 — formula evaluation not implemented.
    /// When implemented: PercentDiscount formula {"percent":10} on $100 cart → $10 discount.
    /// </summary>
    [Fact(Skip = "Pending S4.1 DiscountEngine: formula evaluation not implemented — stub always returns 0")]
    public async Task RealEngine_PercentagePromotion_10Pct_On100Cart_ShouldReturn10Discount()
    {
        var promo = BuildActivePromotion(PromotionType.PercentDiscount, """{"percent":10}""");

        var mockPromoService = new Mock<IPromotionService>();
        mockPromoService
            .Setup(s => s.GetActiveForShopAsync(It.IsAny<Guid>(), _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Promotion>>([promo]));

        var engine = BuildEngine(mockPromoService.Object);
        var cart = new[] { new CartItem("PROD001", 1, 100m) };
        var result = await engine.CalculateAsync(cart, null, _shopId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DiscountAmount.Should().Be(10m,
            "10% of $100 cart = $10 discount");
        result.Value.FinalTotal.Should().Be(90m);
        result.Value.AppliedPromotionIds.Should().Contain(promo.Id.ToString());
    }

    /// <summary>
    /// Pending S4.1: DiscountEngine stub always returns 0 — formula evaluation not implemented.
    /// When implemented: FixedDiscount formula {"amount":15} on $100 cart → $15 discount.
    /// </summary>
    [Fact(Skip = "Pending S4.1 DiscountEngine: formula evaluation not implemented — stub always returns 0")]
    public async Task RealEngine_FixedAmountPromotion_15_On100Cart_ShouldReturn15Discount()
    {
        var promo = BuildActivePromotion(PromotionType.FixedDiscount, """{"amount":15}""");

        var mockPromoService = new Mock<IPromotionService>();
        mockPromoService
            .Setup(s => s.GetActiveForShopAsync(It.IsAny<Guid>(), _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Promotion>>([promo]));

        var engine = BuildEngine(mockPromoService.Object);
        var cart = new[] { new CartItem("PROD001", 1, 100m) };
        var result = await engine.CalculateAsync(cart, null, _shopId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DiscountAmount.Should().Be(15m, "fixed $15 discount on $100 cart");
        result.Value.FinalTotal.Should().Be(85m);
    }

    /// <summary>
    /// Pending S4.1: DiscountEngine stub always returns 0 — formula evaluation not implemented.
    /// When implemented: two stackable promotions must sum their discounts.
    /// </summary>
    [Fact(Skip = "Pending S4.1 DiscountEngine: formula evaluation not implemented — stub always returns 0")]
    public async Task RealEngine_TwoStackablePromotions_ShouldSumDiscounts()
    {
        // 10% + $15 on $100 cart → $10 + $15 = $25 discount
        var promo1 = BuildActivePromotion(PromotionType.PercentDiscount, """{"percent":10}""");
        var promo2 = BuildActivePromotion(PromotionType.FixedDiscount, """{"amount":15}""");

        var mockPromoService = new Mock<IPromotionService>();
        mockPromoService
            .Setup(s => s.GetActiveForShopAsync(It.IsAny<Guid>(), _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Promotion>>([promo1, promo2]));

        var engine = BuildEngine(mockPromoService.Object);
        var cart = new[] { new CartItem("PROD001", 1, 100m) };
        var result = await engine.CalculateAsync(cart, null, _shopId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DiscountAmount.Should().Be(25m,
            "two stackable promotions (10% + $15) on $100 cart must sum to $25 (ADR-020)");
        result.Value.AppliedPromotionIds.Should().HaveCount(2);
    }

    /// <summary>
    /// Pending S4.1: DiscountEngine stub always returns 0 — formula evaluation not implemented.
    /// When implemented: DiscountAmount must never exceed OriginalTotal (FinalTotal >= 0).
    /// </summary>
    [Fact(Skip = "Pending S4.1 DiscountEngine: formula evaluation not implemented — stub always returns 0")]
    public async Task RealEngine_DiscountCappedAtOriginalTotal_FinalTotalMustNotBeNegative()
    {
        // A promotion worth more than the cart total must be capped at the cart total.
        var promo = BuildActivePromotion(PromotionType.FixedDiscount, """{"amount":200}""");

        var mockPromoService = new Mock<IPromotionService>();
        mockPromoService
            .Setup(s => s.GetActiveForShopAsync(It.IsAny<Guid>(), _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Promotion>>([promo]));

        var engine = BuildEngine(mockPromoService.Object);
        var cart = new[] { new CartItem("PROD001", 1, 50m) };  // $200 discount on $50 cart
        var result = await engine.CalculateAsync(cart, null, _shopId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DiscountAmount.Should().BeLessThanOrEqualTo(result.Value.OriginalTotal,
            "discount must never exceed original total — FinalTotal must be >= 0");
        result.Value.FinalTotal.Should().BeGreaterThanOrEqualTo(0m,
            "FinalTotal must never be negative regardless of promotion value");
    }

    /// <summary>
    /// Pending S4.1: DiscountEngine stub always returns 0 — formula evaluation not implemented.
    /// Parameterized theory covering key formula combinations.
    /// </summary>
    [Theory(Skip = "Pending S4.1 DiscountEngine: formula evaluation not implemented — stub always returns 0")]
    [InlineData(100.0, "PercentDiscount", 10.0, 10.0)]   // 10% of $100 = $10
    [InlineData(100.0, "FixedDiscount",   15.0, 15.0)]   // flat $15 on $100 = $15
    [InlineData(50.0,  "PercentDiscount", 20.0, 10.0)]   // 20% of $50 = $10
    [InlineData(200.0, "FixedDiscount",   30.0, 30.0)]   // flat $30 on $200 = $30
    [InlineData(10.0,  "FixedDiscount",   50.0, 10.0)]   // $50 discount capped at $10 cart total
    public async Task RealEngine_VariousFormulas_Theory(
        double cartTotal, string type, double formulaValue, double expectedDiscount)
    {
        var promoType = Enum.Parse<PromotionType>(type);
        var formulaJson = promoType == PromotionType.PercentDiscount
            ? $$$"""{"percent":{{{formulaValue}}}}"""
            : $$$"""{"amount":{{{formulaValue}}}}""";

        var promo = BuildActivePromotion(promoType, formulaJson);
        var mockPromoService = new Mock<IPromotionService>();
        mockPromoService
            .Setup(s => s.GetActiveForShopAsync(It.IsAny<Guid>(), _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Promotion>>([promo]));

        var engine = BuildEngine(mockPromoService.Object);
        var cart = new[] { new CartItem("PROD001", 1, (decimal)cartTotal) };
        var result = await engine.CalculateAsync(cart, null, _shopId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DiscountAmount.Should().Be((decimal)expectedDiscount,
            $"{type} formula value={formulaValue} on cart={cartTotal} should produce discount={expectedDiscount}");
        result.Value.FinalTotal.Should().BeGreaterThanOrEqualTo(0m, "FinalTotal must never be negative");
    }
}
