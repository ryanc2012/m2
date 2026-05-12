using M2.Domain.GoodsReceipt;

namespace M2.M2PortalBff.Endpoints;

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

        group.MapGet("/", async (
            Guid tenantId,
            Guid shopId,
            int page,
            int pageSize,
            IGoodsReceiptService svc) =>
        {
            var result = await svc.ListByShopAsync(tenantId, shopId, page, pageSize);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/confirm", async (
            Guid id,
            IGoodsReceiptService svc) =>
        {
            var result = await svc.ConfirmAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/discrepancy", async (
            Guid id,
            RecordDiscrepancyRequest req,
            IGoodsReceiptService svc) =>
        {
            var result = await svc.RecordDiscrepancyAsync(id, req.LineItemId, req.ReceivedQty, req.DiscrepancyNote);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/post-to-sap", async (
            Guid id,
            IGoodsReceiptService svc) =>
        {
            var result = await svc.PostToSapAsync(id);
            return result.IsSuccess ? Results.Accepted() : Results.BadRequest(result.Error);
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
