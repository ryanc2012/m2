using M2.Domain.GoodsReceipt;

namespace M2.MekaPosBff.Endpoints;

public static class GoodsReceiptEndpoints
{
    public static IEndpointRouteBuilder MapGoodsReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/goods-receipts").WithTags("GoodsReceipt");

        group.MapPost("/", async (
            CreateGrnRequest req,
            IGoodsReceiptService svc) =>
        {
            var lineItems = req.LineItems.Select(l =>
                new GoodsReceiptLineItemRequest(l.ProductCode, l.ProductNameEn, l.ProductNameZht,
                    l.ExpectedQty, l.UnitOfMeasure));
            var result = await svc.CreateAsync(req.TenantId, req.ShopId, req.SapDeliveryNoteNumber, lineItems);
            return result.IsSuccess
                ? Results.Created($"/goods-receipts/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IGoodsReceiptService svc) =>
        {
            var result = await svc.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapPost("/{id:guid}/discrepancy", async (
            Guid id,
            RecordDiscrepancyRequest req,
            IGoodsReceiptService svc) =>
        {
            var result = await svc.RecordDiscrepancyAsync(id, req.LineItemId, req.ReceivedQty, req.DiscrepancyNote);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return app;
    }
}

public record CreateGrnRequest(
    Guid TenantId,
    Guid ShopId,
    string SapDeliveryNoteNumber,
    List<GrnLineItemDto> LineItems);

public record GrnLineItemDto(
    string ProductCode,
    string ProductNameEn,
    string ProductNameZht,
    decimal ExpectedQty,
    string UnitOfMeasure);

public record RecordDiscrepancyRequest(
    Guid LineItemId,
    decimal ReceivedQty,
    string DiscrepancyNote);
