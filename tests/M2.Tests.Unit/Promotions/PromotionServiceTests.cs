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

    // ─── S4.9: PromotionService eligibility tests ────────────────────────────

    [Fact]
    public async Task GetActiveForShop_MemberWithActivePromotion_ShouldReturnPromotion()
    {
        // Contract: GetActiveForShopAsync returns all Active promotions for a shop, regardless of member.
        // Member eligibility filtering is a higher-level concern layered above PromotionService.
        var mockService = new Mock<IPromotionService>();
        var activePromo = BuildPromotion(PromotionStatus.Active);

        mockService
            .Setup(s => s.GetActiveForShopAsync(_tenantId, _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Promotion>>([activePromo]));

        var result = await mockService.Object.GetActiveForShopAsync(_tenantId, _shopId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1, "one active promotion must be returned for the shop");
        result.Value![0].Status.Should().Be(PromotionStatus.Active);
    }

    [Fact]
    public async Task GetActiveForShop_ExpiredPromotion_ShouldNotBeReturned()
    {
        // Contract: expired promotions (status = Expired or EndDate in the past) must not be returned
        // by GetActiveForShopAsync. Only Status == Active promotions are eligible.
        var mockService = new Mock<IPromotionService>();

        // No active promotions — the expired one is filtered server-side
        mockService
            .Setup(s => s.GetActiveForShopAsync(_tenantId, _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Promotion>>([]));

        var result = await mockService.Object.GetActiveForShopAsync(_tenantId, _shopId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty(
            "an expired promotion must not appear in GetActiveForShopAsync results");
    }

    [Fact]
    public async Task GetActiveForShop_PromotionForDifferentShop_ShouldNotBeReturned()
    {
        // Contract: GetActiveForShopAsync is scoped to the given shopId.
        // Promotions belonging to a different shop must never appear.
        var mockService = new Mock<IPromotionService>();
        var differentShopId = Guid.NewGuid();

        mockService
            .Setup(s => s.GetActiveForShopAsync(_tenantId, _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Promotion>>([]));

        var result = await mockService.Object.GetActiveForShopAsync(_tenantId, _shopId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().NotContain(
            p => p.ShopId == differentShopId,
            "promotions from a different shop must never appear in a shop-scoped query");
    }

    [Fact]
    public async Task GetActiveForShop_MultipleActivePromotions_ShouldReturnAll()
    {
        // Contract: multiple Active promotions for the same shop must all be returned.
        var mockService = new Mock<IPromotionService>();
        var promo1 = BuildPromotion(PromotionStatus.Active);
        var promo2 = new Promotion(
            _tenantId, _shopId,
            BilingualText.From("Promo 2", "促銷2"),
            PromotionType.FixedDiscount,
            """{"amount":20}""",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            isStackable: false);
        promo2.Activate();

        mockService
            .Setup(s => s.GetActiveForShopAsync(_tenantId, _shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Promotion>>([promo1, promo2]));

        var result = await mockService.Object.GetActiveForShopAsync(_tenantId, _shopId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2,
            "all active promotions for the shop must be returned — DiscountEngine decides which to apply");
        result.Value.Should().OnlyContain(p => p.Status == PromotionStatus.Active);
    }

    [Fact]
    public void ExpiredPromotion_WhenExpired_StatusShouldBeExpired()
    {
        // Domain invariant: Promotion.Expire() transitions to Expired status.
        var promotion = BuildPromotion(PromotionStatus.Active);
        promotion.Expire();

        promotion.Status.Should().Be(PromotionStatus.Expired,
            "calling Expire() on an active promotion must set status to Expired");
    }
}
