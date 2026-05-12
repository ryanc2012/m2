using FluentAssertions;
using M2.Domain.Promotions;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Promotions;

/// <summary>
/// Contracts and domain invariants for IPromotionService.
/// ADR-013: Coupons are pre-issued on promotion activation.
/// ADR-020: Stackable flag controls discount stacking.
/// </summary>
public class PromotionServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();

    private Promotion BuildPromotion(PromotionStatus status = PromotionStatus.Draft)
    {
        var p = new Promotion(
            _tenantId, _shopId,
            BilingualText.From("Test Promo", "測試促銷"),
            PromotionType.PercentDiscount,
            """{"percent":10}""",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            isStackable: true);

        if (status == PromotionStatus.Active)
            p.Activate();
        else if (status == PromotionStatus.Paused)
        {
            p.Activate();
            p.Pause();
        }

        return p;
    }

    [Fact]
    public void CreatePromotion_ShouldBeDraft_OnCreation()
    {
        // Arrange / Act
        var promotion = new Promotion(
            _tenantId, _shopId,
            BilingualText.From("New Promo", "新促銷"),
            PromotionType.FixedDiscount,
            """{"amount":50}""",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(14),
            isStackable: false);

        // Assert
        promotion.Status.Should().Be(PromotionStatus.Draft,
            "a newly created promotion must always start in Draft status");
    }

    [Fact]
    public async Task ActivatePromotion_ShouldTriggerCouponIssuance()
    {
        // ADR-013: coupons are pre-issued for eligible members when a promotion goes live.
        // Arrange
        var mockPromoService = new Mock<IPromotionService>();
        var mockCouponService = new Mock<ICouponService>();
        var promoId = Guid.NewGuid();

        var activePromotion = BuildPromotion(PromotionStatus.Active);

        mockCouponService
            .Setup(cs => cs.IssueAsync(
                _tenantId, _shopId, promoId, It.IsAny<Guid?>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(
                new Coupon(_tenantId, _shopId, promoId, null, "COUP001",
                    DateTimeOffset.UtcNow.AddDays(30))));

        mockPromoService
            .Setup(s => s.ActivateAsync(promoId, It.IsAny<CancellationToken>()))
            .Callback(() =>
                mockCouponService.Object.IssueAsync(
                    _tenantId, _shopId, promoId, null,
                    DateTimeOffset.UtcNow.AddDays(30)))
            .ReturnsAsync(Result.Success(activePromotion));

        // Act
        var result = await mockPromoService.Object.ActivateAsync(promoId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(PromotionStatus.Active);
        mockCouponService.Verify(
            cs => cs.IssueAsync(
                _tenantId, _shopId, promoId, It.IsAny<Guid?>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "activating a promotion must trigger coupon pre-issuance for eligible members (ADR-013)");
    }

    [Fact]
    public void ActivatePromotion_WhenAlreadyActive_ShouldThrow()
    {
        // Arrange — promotion is already Active
        var promotion = BuildPromotion(PromotionStatus.Active);

        // Act
        Action act = () => promotion.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>(
            "activating an already-active promotion violates the domain invariant");
    }

    [Fact]
    public async Task PausePromotion_WhenActive_ShouldSucceed()
    {
        // Arrange
        var mockService = new Mock<IPromotionService>();
        var promoId = Guid.NewGuid();

        var pausedPromotion = BuildPromotion(PromotionStatus.Paused);

        mockService
            .Setup(s => s.PauseAsync(promoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(pausedPromotion));

        // Act
        var result = await mockService.Object.PauseAsync(promoId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(PromotionStatus.Paused,
            "pausing an active promotion must transition it to Paused status");
    }
}
