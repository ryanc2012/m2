namespace M2.Domain.ApiKeys;

public record ApiKeyDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string[] Scopes,
    bool IsActive,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
