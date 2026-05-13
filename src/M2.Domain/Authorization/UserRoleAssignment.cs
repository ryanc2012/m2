using M2.SharedKernel;

namespace M2.Domain.Authorization;

/// <summary>
/// Time-bounded assignment of an <see cref="AuthorizationRole"/> to a platform user (Entra ID sub/oid).
/// </summary>
public class UserRoleAssignment : BaseEntity
{
    /// <summary>Entra ID subject/oid claim identifying the platform user.</summary>
    public string UserId { get; protected set; } = string.Empty;

    public Guid AuthorizationRoleId { get; protected set; }

    public DateTimeOffset ValidFrom { get; protected set; }

    /// <summary>Null means the assignment does not expire.</summary>
    public DateTimeOffset? ValidTo { get; protected set; }

    protected UserRoleAssignment() { }

    public UserRoleAssignment(
        Guid tenantId,
        Guid shopId,
        string userId,
        Guid authorizationRoleId,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo = null)
    {
        TenantId = tenantId;
        ShopId = shopId;
        UserId = userId;
        AuthorizationRoleId = authorizationRoleId;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public bool IsActive(DateTimeOffset at) =>
        ValidFrom <= at && (ValidTo == null || ValidTo >= at);
}
