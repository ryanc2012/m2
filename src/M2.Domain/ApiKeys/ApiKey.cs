using M2.SharedKernel;
using System.Security.Cryptography;
using System.Text;

namespace M2.Domain.ApiKeys;

public class ApiKey : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;   // SHA-256 hex
    public string[] Scopes { get; private set; } = [];
    public bool IsActive { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    protected ApiKey() { }

    public static (ApiKey key, string plaintext) Create(
        Guid tenantId,
        string name,
        string[] scopes,
        string createdBy,
        DateTimeOffset? expiresAt = null)
    {
        var plaintext = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext))).ToLowerInvariant();
        var key = new ApiKey
        {
            TenantId = tenantId,
            ShopId = Guid.Empty,
            Name = name,
            KeyHash = hash,
            Scopes = scopes,
            IsActive = true,
            ExpiresAt = expiresAt,
            CreatedBy = createdBy
        };
        return (key, plaintext);
    }

    public void Revoke() => IsActive = false;
}
