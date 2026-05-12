using FluentAssertions;
using M2.Domain.Reporting;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Reporting;

/// <summary>
/// Contract tests for IReportingService.
/// Domain objects are mocked — no live DB or aggregation logic required.
/// </summary>
public class ReportingServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();

    [Fact]
    public async Task GetDailySalesSummary_ReturnsEmptySummaryForNewShop()
    {
        // A shop with no transactions must return a zero-value summary (not null / not failure).
        var mockService = new Mock<IReportingService>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var emptySummary = new SalesSummary(_tenantId, _shopId, today,
            TotalTransactions: 0,
            TotalRevenue: 0m,
            TotalReturns: 0,
            TotalReturnValue: 0m);

        mockService
            .Setup(s => s.GetDailySalesSummaryAsync(_tenantId, _shopId, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(emptySummary));

        var result = await mockService.Object.GetDailySalesSummaryAsync(_tenantId, _shopId, today);

        result.IsSuccess.Should().BeTrue(
            "GetDailySalesSummaryAsync must succeed even when the shop has no transactions yet");
        result.Value!.TotalTransactions.Should().Be(0,
            "a new shop must report zero transactions");
        result.Value.TotalRevenue.Should().Be(0m,
            "a new shop must report zero revenue");
        result.Value.TotalReturns.Should().Be(0,
            "a new shop must report zero returns");
        result.Value.ShopId.Should().Be(_shopId);
        result.Value.Date.Should().Be(today);
    }

    [Fact]
    public async Task GetAttendanceSummary_ReturnsEmptySummaryForNewShop()
    {
        // A shop with no attendance records must return a zero-value summary.
        var mockService = new Mock<IReportingService>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var emptySummary = new AttendanceSummary(_tenantId, _shopId, today,
            TotalClockedIn: 0,
            TotalHours: 0m);

        mockService
            .Setup(s => s.GetAttendanceSummaryAsync(_tenantId, _shopId, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(emptySummary));

        var result = await mockService.Object.GetAttendanceSummaryAsync(_tenantId, _shopId, today);

        result.IsSuccess.Should().BeTrue(
            "GetAttendanceSummaryAsync must succeed even when no employees have clocked in yet");
        result.Value!.TotalClockedIn.Should().Be(0,
            "a new shop must report zero employees clocked in");
        result.Value.TotalHours.Should().Be(0m,
            "a new shop must report zero total hours");
        result.Value.ShopId.Should().Be(_shopId);
        result.Value.Date.Should().Be(today);
    }

    [Fact]
    public async Task GetPromotionPerformance_ReturnsResultForKnownPromotion()
    {
        // A known promotion must return a non-null performance record.
        var mockService = new Mock<IReportingService>();
        var promotionId = Guid.NewGuid();
        var performance = new PromotionPerformance(
            promotionId,
            new BilingualText("Summer Sale", "夏日优惠"),
            CouponsIssued: 500,
            CouponsRedeemed: 123,
            TotalDiscountGiven: 6150m);

        mockService
            .Setup(s => s.GetPromotionPerformanceAsync(_tenantId, _shopId, promotionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(performance));

        var result = await mockService.Object.GetPromotionPerformanceAsync(_tenantId, _shopId, promotionId);

        result.IsSuccess.Should().BeTrue(
            "GetPromotionPerformanceAsync must succeed for a known promotion ID");
        result.Value!.PromotionId.Should().Be(promotionId,
            "the returned performance must reference the requested promotion");
        result.Value.CouponsIssued.Should().Be(500,
            "the coupon issuance count must match what was recorded");
        result.Value.CouponsRedeemed.Should().Be(123,
            "the redemption count must reflect actual redemptions");
        result.Value.TotalDiscountGiven.Should().Be(6150m,
            "the total discount must sum all redeemed coupon values");
    }
}
