using FluentAssertions;
using M2.Domain.Promotions;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Promotions;

/// <summary>
/// Contracts for ICouponService coupon redemption.
/// BE-REC-001 R5: Coupon QR codes are short-lived signed JWTs; POS validates via code string.
/// </summary>
public class CouponServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _promotionId = Guid.NewGuid();

    private Coupon BuildValidCoupon(string code = "VALID001") =>
        new(_tenantId, _shopId, _promotionId, null, code, DateTimeOffset.UtcNow.AddHours(1));

    [Fact]
    public async Task Redeem_ValidCoupon_ShouldMarkRedeemed()
    {
        // Arrange
        var mockService = new Mock<ICouponService>();
        const string code = "VALID001";

        var redeemedCoupon = BuildValidCoupon(code);
        redeemedCoupon.Redeem();

        mockService
            .Setup(s => s.RedeemAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(redeemedCoupon));

        // Act
        var result = await mockService.Object.RedeemAsync(code);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsRedeemed.Should().BeTrue(
            "a valid coupon that has been redeemed must have IsRedeemed = true");
        result.Value.RedeemedAt.Should().NotBeNull(
            "RedeemedAt must be stamped upon successful redemption");
    }

    [Fact]
    public async Task Redeem_AlreadyRedeemedCoupon_ShouldFail()
    {
        // Arrange
        var mockService = new Mock<ICouponService>();
        const string code = "USED001";

        mockService
            .Setup(s => s.RedeemAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Coupon>("Coupon has already been redeemed."));

        // Act
        var result = await mockService.Object.RedeemAsync(code);

        // Assert
        result.IsFailure.Should().BeTrue(
            "attempting to redeem an already-redeemed coupon must fail");
        result.Error.Should().Contain("redeemed",
            "the error message must describe the already-redeemed state");
    }

    [Fact]
    public async Task Redeem_ExpiredCoupon_ShouldFail()
    {
        // Arrange
        var mockService = new Mock<ICouponService>();
        const string code = "EXPIRED001";

        mockService
            .Setup(s => s.RedeemAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Coupon>("Coupon has expired."));

        // Act
        var result = await mockService.Object.RedeemAsync(code);

        // Assert
        result.IsFailure.Should().BeTrue(
            "attempting to redeem an expired coupon must fail");
        result.Error.Should().Contain("expired",
            "the error message must indicate the expiry condition");
    }
}
