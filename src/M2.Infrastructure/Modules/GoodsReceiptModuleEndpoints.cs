using M2.Domain.GoodsReceipt;
using M2.Domain.GoodsReceipt.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace M2.Infrastructure.Modules;

public static class GoodsReceiptModuleEndpoints
{
    public static IEndpointRouteBuilder MapGoodsReceiptModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/modules/goods-receipt")
            .RequireAuthorization()
            .WithTags("Modules.GoodsReceipt");

        group.MapPost("/", async (CreateGoodsReceiptPayload payload, IGoodsReceiptService svc) =>
        {
            var lineItems = payload.LineItems.Select(l =>
                new GoodsReceiptLineItemRequest(l.ProductCode, l.ProductNameEn, l.ProductNameZht, l.ExpectedQty, l.UnitOfMeasure));
            var result = await svc.CreateAsync(payload.TenantId, payload.ShopId, payload.SapDeliveryNoteNumber, lineItems);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}", async (Guid id, IGoodsReceiptService svc) =>
        {
            var result = await svc.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.NotFound(result.Error);
        });

        group.MapGet("/", async (Guid tenantId, Guid shopId, int page, int pageSize, IGoodsReceiptService svc) =>
        {
            var result = await svc.ListByShopAsync(tenantId, shopId, page, pageSize);
            if (!result.IsSuccess) return Results.BadRequest(result.Error);
            return Results.Ok(result.Value!.Select(ToDto).ToList());
        });

        group.MapPost("/{id:guid}/confirm", async (Guid id, IGoodsReceiptService svc) =>
        {
            var result = await svc.ConfirmAsync(id);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/discrepancy", async (Guid id, RecordDiscrepancyPayload payload, IGoodsReceiptService svc) =>
        {
            var result = await svc.RecordDiscrepancyAsync(id, payload.LineItemId, payload.ReceivedQty, payload.DiscrepancyNote);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/post-to-sap", async (Guid id, IGoodsReceiptService svc) =>
        {
            var result = await svc.PostToSapAsync(id);
            return result.IsSuccess ? Results.Accepted() : Results.BadRequest(result.Error);
        });

        return app;
    }

    private static GoodsReceiptNoteDto ToDto(GoodsReceiptNote g) => new(
        g.Id, g.TenantId, g.ShopId,
        g.SapDeliveryNoteNumber, g.Status.ToString(),
        g.ReceivedAt, g.ConfirmedAt, g.DiscrepancyNote);
}
