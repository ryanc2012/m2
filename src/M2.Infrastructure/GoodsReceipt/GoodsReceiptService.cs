using M2.Domain.GoodsReceipt;
using M2.SapConnector;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace M2.Infrastructure.GoodsReceipt;

/// <summary>In-memory stub GoodsReceiptService. EF wiring deferred post-Sprint 4.
/// PostToSapAsync now enqueues via IOutboxService (SAP outbox pattern, ADR-006/ADR-017).</summary>
internal sealed class GoodsReceiptService(
    IOutboxService outboxService,
    ILogger<GoodsReceiptService> logger) : IGoodsReceiptService
{
    private static readonly Dictionary<Guid, GoodsReceiptNote> _store = [];

    public Task<Result<GoodsReceiptNote>> CreateAsync(
        Guid tenantId, Guid shopId, string sapDeliveryNoteNumber,
        IEnumerable<GoodsReceiptLineItemRequest> lineItems, CancellationToken ct = default)
    {
        var note = new GoodsReceiptNote(tenantId, shopId, sapDeliveryNoteNumber);

        foreach (var req in lineItems)
        {
            var item = new GoodsReceiptLineItem(
                tenantId, shopId, note.Id,
                req.ProductCode,
                BilingualText.From(req.ProductNameEn, req.ProductNameZht),
                req.ExpectedQty,
                receivedQty: 0,
                req.UnitOfMeasure);
            note.AddLineItem(item);
        }

        _store[note.Id] = note;
        logger.LogInformation("GRN created: id={Id} deliveryNote={SapDN} tenant={TenantId}",
            note.Id, sapDeliveryNoteNumber, tenantId);
        return Task.FromResult(Result.Success(note));
    }

    public Task<Result<GoodsReceiptNote>> ConfirmAsync(Guid id, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(id, out var note))
            return Task.FromResult(Result.Failure<GoodsReceiptNote>($"GRN {id} not found."));

        note.Confirm();
        logger.LogInformation("GRN confirmed: id={Id}", id);
        return Task.FromResult(Result.Success(note));
    }

    public Task<Result<GoodsReceiptNote>> RecordDiscrepancyAsync(
        Guid id, Guid lineItemId, decimal receivedQty, string discrepancyNote, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(id, out var note))
            return Task.FromResult(Result.Failure<GoodsReceiptNote>($"GRN {id} not found."));

        var line = note.LineItems.FirstOrDefault(l => l.Id == lineItemId);
        if (line is null)
            return Task.FromResult(Result.Failure<GoodsReceiptNote>($"Line item {lineItemId} not found on GRN {id}."));

        line.RecordDiscrepancy(receivedQty, discrepancyNote);
        note.MarkDiscrepancy();
        logger.LogWarning("GRN discrepancy recorded: id={Id} lineItem={LineItemId}", id, lineItemId);
        return Task.FromResult(Result.Success(note));
    }

    public Task<Result<GoodsReceiptNote>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _store.TryGetValue(id, out var note)
            ? Task.FromResult(Result.Success(note))
            : Task.FromResult(Result.Failure<GoodsReceiptNote>($"GRN {id} not found."));

    public Task<Result<IReadOnlyList<GoodsReceiptNote>>> ListByShopAsync(
        Guid tenantId, Guid shopId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var results = _store.Values
            .Where(n => n.TenantId == tenantId && n.ShopId == shopId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(Result.Success<IReadOnlyList<GoodsReceiptNote>>(results));
    }

    public async Task<Result> PostToSapAsync(Guid id, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(id, out var note))
            return Result.Failure($"GRN {id} not found.");

        var payload = new SapGoodsMovementPayload(
            DeliveryNoteNumber: note.SapDeliveryNoteNumber,
            MovementType: "101", // Standard GR movement type
            Items: note.LineItems
                .Select(l => new SapGoodsMovementItem(
                    MaterialNumber: l.ProductCode,
                    Quantity: l.ReceivedQty > 0 ? l.ReceivedQty : l.ExpectedQty,
                    UnitOfMeasure: l.UnitOfMeasure,
                    StorageLocation: "0001"))
                .ToList()
                .AsReadOnly());

        await outboxService.EnqueueAsync(payload, ct);

        logger.LogInformation("GRN SAP post enqueued via outbox: id={Id}", id);
        return Result.Success();
    }
}

