namespace M2.Domain.GoodsReceipt.Dtos;

public record GoodsReceiptNoteDto(
    Guid Id,
    Guid TenantId,
    Guid ShopId,
    string SapDeliveryNoteNumber,
    string Status,
    DateTimeOffset? ReceivedAt,
    DateTimeOffset? ConfirmedAt,
    string? DiscrepancyNote);

public record CreateGoodsReceiptPayload(
    Guid TenantId,
    Guid ShopId,
    string SapDeliveryNoteNumber,
    IReadOnlyList<GrnLineItemPayload> LineItems);

public record GrnLineItemPayload(
    string ProductCode,
    string ProductNameEn,
    string ProductNameZht,
    decimal ExpectedQty,
    string UnitOfMeasure);

public record RecordDiscrepancyPayload(
    Guid LineItemId,
    decimal ReceivedQty,
    string DiscrepancyNote);
