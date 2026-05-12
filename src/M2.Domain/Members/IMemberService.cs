using M2.SharedKernel;

namespace M2.Domain.Members;

public interface IMemberService
{
    Task<Result<Member>> RegisterAsync(
        Guid tenantId,
        Guid shopId,
        BilingualText firstName,
        BilingualText lastName,
        string phone,
        string? email,
        MembershipTier membershipTier,
        CancellationToken ct = default);

    Task<Result<Member>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Server-side QR lookup — QR contains reference ID only (ADR-019).</summary>
    Task<Result<Member>> GetByQrCodeAsync(string qrCode, CancellationToken ct = default);

    Task<Result<Member>> UpdateProfileAsync(
        Guid id,
        BilingualText firstName,
        BilingualText lastName,
        string? email,
        CancellationToken ct = default);

    Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default);
}
