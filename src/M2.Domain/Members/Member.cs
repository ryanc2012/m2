using M2.SharedKernel;

namespace M2.Domain.Members;

public class Member : BaseEntity
{
    public BilingualText FirstName { get; protected set; } = BilingualText.Empty;
    public BilingualText LastName { get; protected set; } = BilingualText.Empty;
    public string Phone { get; protected set; } = string.Empty;
    public string? Email { get; protected set; }
    /// <summary>Opaque reference ID embedded in QR code (ADR-019: server-side lookup).</summary>
    public string QrCode { get; protected set; } = string.Empty;
    public MembershipTier MembershipTier { get; protected set; } = MembershipTier.Standard;
    public DateTimeOffset JoinedAt { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    private readonly List<OtpRequest> _otpRequests = [];
    public IReadOnlyCollection<OtpRequest> OtpRequests => _otpRequests.AsReadOnly();

    protected Member() { }

    public Member(
        Guid tenantId,
        Guid shopId,
        BilingualText firstName,
        BilingualText lastName,
        string phone,
        string? email,
        MembershipTier membershipTier)
    {
        TenantId = tenantId;
        ShopId = shopId;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Email = email;
        MembershipTier = membershipTier;
        QrCode = Guid.NewGuid().ToString("N");
        JoinedAt = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public void UpdateProfile(BilingualText firstName, BilingualText lastName, string? email)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
