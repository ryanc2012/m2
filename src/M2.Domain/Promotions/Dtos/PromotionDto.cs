namespace M2.Domain.Promotions.Dtos;

public record PromotionDto(
    Guid Id,
    Guid TenantId,
    Guid ShopId,
    string NameEn,
    string NameZht,
    string Type,
    string Status,
    string FormulaJson,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool IsStackable,
    Guid? ApprovalRequestId);

public record CreatePromotionPayload(
    Guid TenantId,
    Guid ShopId,
    string NameEn,
    string NameZht,
    string Type,
    string FormulaJson,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool IsStackable);
