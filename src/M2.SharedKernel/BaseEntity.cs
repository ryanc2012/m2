namespace M2.SharedKernel;

public abstract class BaseEntity : ITenanted, IShopScoped
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid TenantId { get; protected set; }
    public Guid ShopId { get; protected set; }

    // Audit columns (DB-001)
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    // Soft delete (DB-001)
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }
}
