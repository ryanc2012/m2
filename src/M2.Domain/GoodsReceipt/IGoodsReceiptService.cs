using M2.SharedKernel;

namespace M2.Domain.GoodsReceipt;

public interface IGoodsReceiptService
{
    Task<Result<GoodsReceiptNote>> CreateAsync(
        Guid tenantId,
        Guid shopId,
        string sapDeliveryNoteNumber,
        IEnumerable<GoodsReceiptLineItemRequest> lineItems,
        CancellationToken ct = default);

    Task<Result<GoodsReceiptNote>> ConfirmAsync(
        Guid id,
        CancellationToken ct = default);

    Task<Result<GoodsReceiptNote>> RecordDiscrepancyAsync(
        Guid id,
        Guid lineItemId,
        decimal receivedQty,
        string discrepancyNote,
        CancellationToken ct = default);

    Task<Result<GoodsReceiptNote>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<GoodsReceiptNote>>> ListByShopAsync(
        Guid tenantId,
        Guid shopId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>Enqueues a goods movement post to SAP via the outbox (ADR-017).</summary>
    Task<Result> PostToSapAsync(
        Guid id,
        CancellationToken ct = default);
}

public record GoodsReceiptLineItemRequest(
    string ProductCode,
    string ProductNameEn,
    string ProductNameZht,
    decimal ExpectedQty,
    string UnitOfMeasure);
