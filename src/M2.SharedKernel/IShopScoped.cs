namespace M2.SharedKernel;

/// <summary>Multi-store first-class support (ADR-013 / ADR-012).</summary>
public interface IShopScoped
{
    Guid ShopId { get; }
}
