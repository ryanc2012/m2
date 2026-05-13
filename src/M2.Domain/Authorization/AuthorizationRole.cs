using M2.SharedKernel;

namespace M2.Domain.Authorization;

/// <summary>
/// Named role that aggregates a set of SAP-style authorization objects (ADR-004).
/// Roles are tenant-scoped; ShopId is set to Guid.Empty for tenant-wide roles.
/// </summary>
public class AuthorizationRole : BaseEntity
{
    public string Name { get; protected set; } = string.Empty;
    public string Description { get; protected set; } = string.Empty;

    private readonly List<RoleAuthorizationObject> _authorizationObjects = [];
    public IReadOnlyCollection<RoleAuthorizationObject> AuthorizationObjects => _authorizationObjects.AsReadOnly();

    protected AuthorizationRole() { }

    public AuthorizationRole(Guid tenantId, string name, string description)
    {
        TenantId = tenantId;
        ShopId = Guid.Empty;
        Name = name;
        Description = description;
    }
}
