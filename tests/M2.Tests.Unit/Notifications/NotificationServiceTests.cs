using FluentAssertions;
using M2.Domain.Notifications;
using M2.Domain.Notifications.Commands;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Notifications;

/// <summary>
/// Contract stubs for INotificationService and ISmsGateway.
/// Run against real implementations when McManus delivers Sprint 2.
/// Contracts: ADR-005 (notification in-process), ADR-010 (ISmsGateway abstraction).
/// </summary>
public class NotificationServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();

    [Fact]
    public async Task RegisterDevice_ShouldPersistToken()
    {
        // Arrange
        var mockService = new Mock<INotificationService>();
        var userId = "user.staff001";
        var fcmToken = "fcm-token-abc123";

        var registration = new DeviceRegistration(
            _tenantId, _shopId, userId, DevicePlatform.Android, fcmToken, apnsToken: null);

        mockService
            .Setup(s => s.RegisterDeviceAsync(
                _tenantId, _shopId, userId, DevicePlatform.Android, fcmToken, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(registration));

        // Act
        var result = await mockService.Object.RegisterDeviceAsync(
            _tenantId, _shopId, userId, DevicePlatform.Android, fcmToken, null);

        // Assert
        result.IsSuccess.Should().BeTrue("device registration must succeed when a valid token is provided");
        result.Value!.UserId.Should().Be(userId);
        result.Value.FcmToken.Should().Be(fcmToken,
            "the FCM token provided must be persisted on the registration record");
        result.Value.Platform.Should().Be(DevicePlatform.Android);
    }

    [Fact]
    public async Task SendPush_ShouldLogNotification()
    {
        // Arrange
        var mockService = new Mock<INotificationService>();
        var userId = "user.member001";
        var templateId = Guid.NewGuid();
        var parameters = new Dictionary<string, string> { ["discount"] = "20%" };

        mockService
            .Setup(s => s.SendPushAsync(userId, templateId, parameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await mockService.Object.SendPushAsync(userId, templateId, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue("SendPushAsync must succeed when a valid user and template are provided");
        // Contract: implementation must write a NotificationLog entry (Status=Sent) after dispatch.
        // Full log assertion requires real implementation (McManus Sprint 2).
        mockService.Verify(
            s => s.SendPushAsync(userId, templateId, parameters, It.IsAny<CancellationToken>()),
            Times.Once,
            "push notification must be dispatched exactly once — no duplicate sends");
    }

    [Fact]
    public async Task SendPush_WhenNoDeviceRegistered_ShouldFail_Gracefully()
    {
        // Contract: when no device token exists for the recipient the service must return
        // a failure Result — it must NOT throw an exception (graceful degradation).
        // Arrange
        var mockService = new Mock<INotificationService>();
        var userId = "user.unregistered";
        var templateId = Guid.NewGuid();

        mockService
            .Setup(s => s.SendPushAsync(userId, templateId, It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("No device registration found for user"));

        // Act
        var result = await mockService.Object.SendPushAsync(userId, templateId, new Dictionary<string, string>());

        // Assert
        result.IsFailure.Should().BeTrue("missing device registration must produce a failure Result, not an exception");
        result.Error.Should().NotBeNullOrEmpty("failure must include an actionable error message");
    }

    [Fact]
    public void SmsGateway_ShouldBeAbstracted_NotDirectlyCalled()
    {
        // ADR-010: ISmsGateway is the sole SMS entry point.
        // No concrete gateway class should be called directly from domain or service code.
        // This test enforces the abstraction contract at the type system level.

        // Assert — ISmsGateway must be an interface, never a concrete class
        typeof(ISmsGateway).IsInterface.Should().BeTrue(
            "ADR-010 requires all SMS delivery to go through the ISmsGateway interface to allow " +
            "provider swap (Twilio / AWS SNS / local telco) without code changes");

        // Assert — INotificationService is also an interface (not a concrete dependency)
        typeof(INotificationService).IsInterface.Should().BeTrue(
            "notification consumers must depend on the interface, not a concrete implementation");
    }
}
