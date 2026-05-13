using M2.SharedKernel;

namespace M2.Domain.Authorization;

/// <summary>
/// Stores a single field-level value for a <see cref="RoleAuthorizationObject"/>
/// (e.g. FieldName="ACTVT", FieldValue="02").
/// </summary>
public class ObjectFieldValue : BaseEntity
{
    public Guid RoleAuthorizationObjectId { get; protected set; }

    /// <summary>SAP field name within the authorization object (e.g. "ACTVT").</summary>
    public string FieldName { get; protected set; } = string.Empty;

    /// <summary>Permitted value for the field (e.g. "01", "02", "*").</summary>
    public string FieldValue { get; protected set; } = string.Empty;

    protected ObjectFieldValue() { }

    public ObjectFieldValue(Guid tenantId, Guid roleAuthorizationObjectId, string fieldName, string fieldValue)
    {
        TenantId = tenantId;
        ShopId = Guid.Empty;
        RoleAuthorizationObjectId = roleAuthorizationObjectId;
        FieldName = fieldName;
        FieldValue = fieldValue;
    }
}
