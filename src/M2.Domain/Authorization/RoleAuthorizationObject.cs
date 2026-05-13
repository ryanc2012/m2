using M2.SharedKernel;

namespace M2.Domain.Authorization;

/// <summary>
/// Maps an <see cref="AuthorizationRole"/> to a named SAP authorization object (e.g. M_PROMOTION_MANAGE).
/// </summary>
public class RoleAuthorizationObject : BaseEntity
{
    public Guid AuthorizationRoleId { get; protected set; }

    /// <summary>SAP authorization object name (e.g. "M_PROMOTION_MANAGE").</summary>
    public string AuthorizationObject { get; protected set; } = string.Empty;

    private readonly List<ObjectFieldValue> _fieldValues = [];
    public IReadOnlyCollection<ObjectFieldValue> FieldValues => _fieldValues.AsReadOnly();

    protected RoleAuthorizationObject() { }

    public RoleAuthorizationObject(Guid tenantId, Guid authorizationRoleId, string authorizationObject)
    {
        TenantId = tenantId;
        ShopId = Guid.Empty;
        AuthorizationRoleId = authorizationRoleId;
        AuthorizationObject = authorizationObject;
    }
}
