using M2.Domain.Authorization;
using M2.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace M2.Infrastructure.Authorization;

public sealed class AuthorizationService(M2DbContext db, IMemoryCache cache) : IAuthorizationService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<AuthCheckResult> CheckAsync(
        ClaimsPrincipal principal,
        string authObject,
        IEnumerable<string>? fields = null,
        CancellationToken ct = default)
    {
        var userId =
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return AuthCheckResult.Deny;

        var fieldList = fields?.ToList() ?? [];
        var cacheKey = $"authz:{userId}:{authObject}:{string.Join(",", fieldList)}";

        if (cache.TryGetValue(cacheKey, out AuthCheckResult cached))
            return cached;

        var result = await EvaluateAsync(userId, authObject, fieldList, ct);

        cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    private async Task<AuthCheckResult> EvaluateAsync(
        string userId,
        string authObject,
        List<string> fields,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Load all active role assignments for this user in the default tenant
        var roleIds = await db.UserRoleAssignments
            .Where(u =>
                u.UserId == userId &&
                u.TenantId == WellKnownTenants.Default &&
                u.ValidFrom <= now &&
                (u.ValidTo == null || u.ValidTo >= now))
            .Select(u => u.AuthorizationRoleId)
            .ToListAsync(ct);

        if (roleIds.Count == 0)
            return AuthCheckResult.Deny;

        // Find matching RoleAuthorizationObjects for the requested authObject
        var matchingObjects = await db.RoleAuthorizationObjects
            .Include(o => o.FieldValues)
            .Where(o =>
                roleIds.Contains(o.AuthorizationRoleId) &&
                o.AuthorizationObject == authObject)
            .ToListAsync(ct);

        if (matchingObjects.Count == 0)
            return AuthCheckResult.Deny;

        // If no field constraints requested, object-level match is enough
        if (fields.Count == 0)
            return AuthCheckResult.Permit;

        // Verify that at least one matching object satisfies ALL requested field names
        foreach (var obj in matchingObjects)
        {
            var permittedFields = obj.FieldValues.Select(f => f.FieldName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (fields.All(f => permittedFields.Contains(f)))
                return AuthCheckResult.Permit;
        }

        return AuthCheckResult.Deny;
    }
}
