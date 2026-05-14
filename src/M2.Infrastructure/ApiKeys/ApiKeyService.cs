using M2.Domain.ApiKeys;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace M2.Infrastructure.ApiKeys;

public sealed class ApiKeyService(M2DbContext db) : IApiKeyService
{
    public async Task<(ApiKeyDto key, string plaintext)> CreateAsync(
        Guid tenantId,
        string name,
        string[] scopes,
        string createdBy,
        DateTimeOffset? expiresAt,
        CancellationToken ct)
    {
        var (apiKey, plaintext) = ApiKey.Create(tenantId, name, scopes, createdBy, expiresAt);
        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync(ct);
        return (ToDto(apiKey), plaintext);
    }

    public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid tenantId, CancellationToken ct)
    {
        var keys = await db.ApiKeys
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);

        return keys.Select(ToDto).ToList().AsReadOnly();
    }

    public async Task<bool> RevokeAsync(Guid tenantId, Guid keyId, CancellationToken ct)
    {
        var key = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.TenantId == tenantId && k.Id == keyId, ct);

        if (key is null) return false;

        key.Revoke();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ValidateAsync(string plaintext, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext))).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        return await db.ApiKeys
            .AnyAsync(k =>
                k.KeyHash == hash &&
                k.IsActive &&
                (k.ExpiresAt == null || k.ExpiresAt >= now), ct);
    }

    private static ApiKeyDto ToDto(ApiKey k) => new(
        k.Id,
        k.TenantId,
        k.Name,
        k.Scopes,
        k.IsActive,
        k.ExpiresAt,
        k.CreatedAt,
        k.CreatedBy);
}
