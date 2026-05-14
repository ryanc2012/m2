namespace M2.Domain.ApiKeys;

public interface IApiKeyService
{
    Task<(ApiKeyDto key, string plaintext)> CreateAsync(
        Guid tenantId,
        string name,
        string[] scopes,
        string createdBy,
        DateTimeOffset? expiresAt,
        CancellationToken ct);

    Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid tenantId, CancellationToken ct);

    Task<bool> RevokeAsync(Guid tenantId, Guid keyId, CancellationToken ct);

    /// <summary>Validates a plaintext API key by hash lookup. Used by ApiKeyMiddleware.</summary>
    Task<bool> ValidateAsync(string plaintext, CancellationToken ct);
}
