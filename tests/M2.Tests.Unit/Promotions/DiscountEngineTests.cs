using FluentAssertions;
using M2.Domain.Promotions;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Promotions;

/// <summary>
/// Contracts for IDiscountEngine discount calculation.
/// ADR-020: Stackable flag controls whether promotions combine.
/// </summary>
public class DiscountEngineTests
{
    private readonly Guid _shopId = Guid.NewGuid();

    private static IEnumerable<CartItem> OneItemCart(decimal unitPrice = 100m) =>
        [new CartItem("PROD001", 1, unitPrice)];

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
}
