namespace M2.Domain.Sales.Dtos;

public record SalesTransactionDto(
    Guid Id,
    Guid TenantId,
    Guid ShopId,
    Guid? MemberId,
    string CashierId,
    decimal TotalAmount,
    decimal DiscountAmount,
    string PaymentMethod,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? VoidedAt);

public record CreateTransactionPayload(
    Guid TenantId,
    Guid ShopId,
    Guid? MemberId,
    string CashierId,
    string PaymentMethod,
    IReadOnlyList<LineItemPayload> LineItems);

public record LineItemPayload(
    string ProductId,
    string ProductNameEn,
    string ProductNameZht,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount = 0);
