using FluentAssertions;
using M2.Domain.Members;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Members;

/// <summary>
/// Contract stubs for IMemberService and IOtpService.
/// Run against real implementations when McManus delivers Sprint 2.
/// Contracts: ADR-019 (server-side QR lookup), BE-REC-001 R5 (short-lived QR reference).
/// </summary>
public class MemberServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();

    private static BilingualText Name(string en, string zht) => BilingualText.From(en, zht);

    [Fact]
    public async Task Register_ShouldGenerateUniqueQrCode()
    {
        // Contract: each registration produces a distinct, opaque QR reference ID.
        // The QR code itself is an opaque token — POS uses server-side lookup (ADR-019).
        // Arrange
        var mockService = new Mock<IMemberService>();

        var member1 = new Member(_tenantId, _shopId, Name("Alice", "愛麗絲"), Name("Wong", "王"), "+60112345678", null, MembershipTier.Standard);
        var member2 = new Member(_tenantId, _shopId, Name("Bob", "鮑勃"), Name("Lee", "李"), "+60187654321", null, MembershipTier.Silver);

        mockService
            .Setup(s => s.RegisterAsync(_tenantId, _shopId, Name("Alice", "愛麗絲"), Name("Wong", "王"), "+60112345678", null, MembershipTier.Standard, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(member1));
        mockService
            .Setup(s => s.RegisterAsync(_tenantId, _shopId, Name("Bob", "鮑勃"), Name("Lee", "李"), "+60187654321", null, MembershipTier.Silver, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(member2));

        // Act
        var result1 = await mockService.Object.RegisterAsync(_tenantId, _shopId, Name("Alice", "愛麗絲"), Name("Wong", "王"), "+60112345678", null, MembershipTier.Standard);
        var result2 = await mockService.Object.RegisterAsync(_tenantId, _shopId, Name("Bob", "鮑勃"), Name("Lee", "李"), "+60187654321", null, MembershipTier.Silver);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value!.QrCode.Should().NotBe(result2.Value!.QrCode,
            "every member registration must produce a globally unique QR reference token");
        result1.Value.QrCode.Should().NotBeNullOrEmpty("QR code must be generated on registration");
        result2.Value.QrCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicatePhone_ShouldFail()
    {
        // Contract: phone is the unique identity key — duplicate registrations must be rejected.
        // Arrange
        var mockService = new Mock<IMemberService>();
        const string duplicatePhone = "+60112345678";

        var existingMember = new Member(_tenantId, _shopId, Name("Alice", "愛麗絲"), Name("Wong", "王"), duplicatePhone, null, MembershipTier.Standard);

        mockService
            .SetupSequence(s => s.RegisterAsync(_tenantId, _shopId, It.IsAny<BilingualText>(), It.IsAny<BilingualText>(), duplicatePhone, It.IsAny<string?>(), It.IsAny<MembershipTier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(existingMember))
            .ReturnsAsync(Result.Failure<Member>("Phone number is already registered"));

        // Act — first registration succeeds
        var first = await mockService.Object.RegisterAsync(_tenantId, _shopId, Name("Alice", "愛麗絲"), Name("Wong", "王"), duplicatePhone, null, MembershipTier.Standard);
        // Second registration with same phone must fail
        var second = await mockService.Object.RegisterAsync(_tenantId, _shopId, Name("Alice2", "愛麗絲2"), Name("Wong", "王"), duplicatePhone, null, MembershipTier.Standard);

        // Assert
        first.IsSuccess.Should().BeTrue("first registration with new phone must succeed");
        second.IsFailure.Should().BeTrue("duplicate phone registration must be rejected");
        second.Error.Should().NotBeNullOrEmpty("failure must carry a descriptive error for the caller");
    }

    [Fact]
    public async Task GetByQrCode_ShouldReturnMember()
    {
        // ADR-019: QR code contains a reference ID only; POS calls this API for server-side lookup.
        // Contract: given a valid QR reference, the correct member record is returned.
        // Arrange
        var mockService = new Mock<IMemberService>();

        var member = new Member(_tenantId, _shopId, Name("Carol", "卡羅爾"), Name("Tan", "陳"), "+60123456789", null, MembershipTier.Gold);
        var qrCode = member.QrCode;

        mockService
            .Setup(s => s.GetByQrCodeAsync(qrCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(member));

        // Act
        var result = await mockService.Object.GetByQrCodeAsync(qrCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.QrCode.Should().Be(qrCode,
            "server-side lookup must return the member whose QR reference matches (ADR-019)");
        result.Value.Phone.Should().Be("+60123456789");
    }

    [Fact]
    public async Task ValidateOtp_WithExpiredCode_ShouldFail()
    {
        // Contract: an OTP past its ExpiresAt timestamp must not validate successfully.
        // Arrange
        var mockOtpService = new Mock<IOtpService>();
        var memberId = Guid.NewGuid();
        const string expiredCode = "999111";

        // OTP expired — service returns false (not a system error, just invalid)
        mockOtpService
            .Setup(s => s.ValidateAsync(memberId, expiredCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        // Act
        var result = await mockOtpService.Object.ValidateAsync(memberId, expiredCode);

        // Assert
        result.IsSuccess.Should().BeTrue("expired OTP is a business validation failure, not a system error");
        result.Value.Should().BeFalse("an expired OTP code must not validate");
    }

    [Fact]
    public async Task ValidateOtp_WithUsedCode_ShouldFail()
    {
        // Contract: a consumed OTP must not be redeemable a second time.
        // Arrange
        var mockOtpService = new Mock<IOtpService>();
        var memberId = Guid.NewGuid();
        const string usedCode = "888222";

        mockOtpService
            .Setup(s => s.ValidateAsync(memberId, usedCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        // Act
        var result = await mockOtpService.Object.ValidateAsync(memberId, usedCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse("a previously used OTP code must not validate (no replay)");

        // Domain invariant verification: OtpRequest.IsValid() returns false for a used code
        var usedOtp = new OtpRequest(memberId, usedCode, TimeSpan.FromMinutes(5));
        usedOtp.MarkUsed();
        usedOtp.IsValid().Should().BeFalse("OtpRequest.IsValid() must return false once MarkUsed() has been called");
    }

    [Fact]
    public async Task ValidateOtp_WithValidCode_ShouldSucceed_AndMarkUsed()
    {
        // Contract: a valid (non-expired, non-used) OTP must validate and be consumed (marked used)
        // so it cannot be replayed.
        // Arrange
        var mockOtpService = new Mock<IOtpService>();
        var memberId = Guid.NewGuid();
        const string validCode = "777333";

        mockOtpService
            .Setup(s => s.ValidateAsync(memberId, validCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        // Act
        var result = await mockOtpService.Object.ValidateAsync(memberId, validCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue("a valid, unused, non-expired OTP must validate successfully");

        // Domain invariant: OtpRequest.IsValid() + MarkUsed contract
        var validOtp = new OtpRequest(memberId, validCode, TimeSpan.FromMinutes(5));
        validOtp.IsValid().Should().BeTrue("fresh OTP within TTL must be valid");

        validOtp.MarkUsed();
        validOtp.IsValid().Should().BeFalse(
            "after ValidateAsync succeeds the implementation must call MarkUsed() — subsequent IsValid() must return false");

        // Implementation note for McManus: after returning true, ApprovalService must call otp.MarkUsed()
        // and persist the change. This prevents replay attacks.
    }
}
