using M2.Domain.Members;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Members;

/// <summary>
/// In-memory stub MemberService. EF DbContext wiring deferred to Sprint 3 (when migrations run).
/// </summary>
internal sealed class MemberService(ILogger<MemberService> logger) : IMemberService
{
    private static readonly Dictionary<Guid, Member> _members = [];

    public Task<Result<Member>> RegisterAsync(
        Guid tenantId, Guid shopId,
        BilingualText firstName, BilingualText lastName,
        string phone, string? email, MembershipTier membershipTier,
        CancellationToken ct = default)
    {
        var member = new Member(tenantId, shopId, firstName, lastName, phone, email, membershipTier);
        _members[member.Id] = member;

        logger.LogInformation(
            "Member registered: id={Id} phone={Phone} tenant={TenantId}",
            member.Id, phone, tenantId);

        return Task.FromResult(Result.Success(member));
    }

    public Task<Result<Member>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _members.TryGetValue(id, out var member)
            ? Task.FromResult(Result.Success(member))
            : Task.FromResult(Result.Failure<Member>($"Member {id} not found."));
    }

    public Task<Result<Member>> GetByQrCodeAsync(string qrCode, CancellationToken ct = default)
    {
        var member = _members.Values.FirstOrDefault(m => m.QrCode == qrCode);
        return member is not null
            ? Task.FromResult(Result.Success(member))
            : Task.FromResult(Result.Failure<Member>($"Member with QR code '{qrCode}' not found."));
    }

    public Task<Result<Member>> UpdateProfileAsync(
        Guid id, BilingualText firstName, BilingualText lastName, string? email,
        CancellationToken ct = default)
    {
        if (!_members.TryGetValue(id, out var member))
            return Task.FromResult(Result.Failure<Member>($"Member {id} not found."));

        member.UpdateProfile(firstName, lastName, email);
        logger.LogInformation("Member updated: id={Id}", id);
        return Task.FromResult(Result.Success(member));
    }

    public Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        if (!_members.TryGetValue(id, out var member))
            return Task.FromResult(Result.Failure($"Member {id} not found."));

        member.Deactivate();
        logger.LogInformation("Member deactivated: id={Id}", id);
        return Task.FromResult(Result.Success());
    }
}
