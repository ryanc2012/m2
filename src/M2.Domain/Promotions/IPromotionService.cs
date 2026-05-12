using M2.SharedKernel;

namespace M2.Domain.Promotions;

public interface IPromotionService
{
    Task<Result<Promotion>> CreateAsync(
        Guid tenantId,
        Guid shopId,
        BilingualText name,
        PromotionType type,
        string formulaJson,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        bool isStackable,
        CancellationToken ct = default);

    Task<Result<Promotion>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Activates the promotion and triggers coupon pre-issuance for eligible members (ADR-013).</summary>
    Task<Result<Promotion>> ActivateAsync(Guid id, CancellationToken ct = default);

    Task<Result<Promotion>> PauseAsync(Guid id, CancellationToken ct = default);

    Task<Result<IReadOnlyList<Promotion>>> GetActiveForShopAsync(
        Guid tenantId,
        Guid shopId,
        CancellationToken ct = default);
}
