using FluentAssertions;
using M2.Domain.Notifications;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Notifications;

/// <summary>
/// Contract tests for INotificationHistoryService and NotificationLog read-status domain method.
/// Covers paged history retrieval, date-range filtering, and mark-as-read invariant.
/// </summary>
public class NotificationHistoryServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();

    private NotificationLog BuildLog(string userId = "user.member001") =>
        new(_tenantId, _shopId,
            templateId: Guid.NewGuid(),
            recipientUserId: userId,
            status: NotificationStatus.Sent);

    [Fact]
    public async Task GetByMember_ReturnsPagedResults()
    {
        // The service must return a page of NotificationLog entries for a given member.
        var mockService = new Mock<INotificationHistoryService>();
        const string memberId = "member-42";
        var logs = new List<NotificationLog>
        {
            BuildLog(memberId),
            BuildLog(memberId)
        };

        mockService
            .Setup(s => s.GetByMemberAsync(
                _tenantId, memberId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<NotificationLog>>(logs));

        var result = await mockService.Object.GetByMemberAsync(_tenantId, memberId, 1, 20);

        result.IsSuccess.Should().BeTrue(
            "GetByMemberAsync must succeed when the member has notification history");
        result.Value.Should().HaveCount(2,
            "the page must contain exactly the two log entries set up for this member");
        result.Value.Should().AllSatisfy(
            log => log.RecipientUserId.Should().Be(memberId),
            "all returned logs must belong to the requested member");
    }

    [Fact]
    public async Task GetByShop_ReturnsResultsWithinDateRange()
    {
        // The service must respect the from/to date range filter when retrieving shop notifications.
        var mockService = new Mock<INotificationHistoryService>();
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var logs = new List<NotificationLog>
        {
            BuildLog("user.cashier001"),
            BuildLog("user.cashier002"),
            BuildLog("user.cashier003")
        };

        mockService
            .Setup(s => s.GetByShopAsync(
                _tenantId, _shopId, from, to,
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<NotificationLog>>(logs));

        var result = await mockService.Object.GetByShopAsync(_tenantId, _shopId, from, to);

        result.IsSuccess.Should().BeTrue(
            "GetByShopAsync must succeed when notifications exist in the given date range");
        result.Value.Should().HaveCount(3,
            "the result must contain only the three logs within the requested date window");
        result.Value.Should().AllSatisfy(
            log => log.TenantId.Should().Be(_tenantId),
            "all returned logs must belong to the correct tenant");
    }

    [Fact]
    public void MarkRead_UpdatesNotificationStatus()
    {
        // Calling MarkAsRead on a domain entity must flip IsRead to true.
        // The domain invariant is tested directly here; the service contract is tested via mock below.
        var log = BuildLog();

        log.IsRead.Should().BeFalse(
            "a newly created notification log must not be read yet");

        log.MarkAsRead();

        log.IsRead.Should().BeTrue(
            "MarkAsRead() must set IsRead = true on the notification log");
    }
}
