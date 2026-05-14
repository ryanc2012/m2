using System.Security.Claims;

namespace M2.Domain.Authorization;

public interface IAuthorizationService
{
    Task<AuthCheckResult> CheckAsync(
        ClaimsPrincipal principal,
        string authObject,
        IEnumerable<string>? fields = null,
        CancellationToken ct = default);
}

public enum AuthCheckResult
{
    Permit,
    Deny
}
