namespace M2.Domain.Sales.Dtos;

public record ReturnTransactionDto(
    Guid Id,
    Guid TenantId,
    Guid ShopId,
    Guid OriginalTransactionId,
    string Reason,
    decimal RefundAmount,
    string RefundMethod,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    bool IsComplete);

public record InitiateReturnPayload(
    Guid TenantId,
    Guid ShopId,
    Guid OriginalTransactionId,
    string Reason,
    decimal RefundAmount);
