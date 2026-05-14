using M2.Domain.Authorization;
using M2.Domain.GoodsReceipt.Dtos;
using M2.Infrastructure.InterModule.Interfaces;
using System.Security.Claims;

namespace M2.MekaPosBff.Endpoints;

public static class GoodsReceiptEndpoints
{
    public static IEndpointRouteBuilder MapGoodsReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/goods-receipts").WithTags("GoodsReceipt");

        group.MapPost("/", async (
            CreateGrnRequest req,
            IAuthorizationService authz,
            ClaimsPrincipal user,
            IGoodsReceiptModuleClient client) =>
        {
            if (await authz.CheckAsync(user, "M_GOODS_RECEIPT_CREATE") == AuthCheckResult.Deny)
                return Results.Forbid();

            var payload = new CreateGoodsReceiptPayload(
                req.TenantId, req.ShopId, req.SapDeliveryNoteNumber,
                req.LineItems.Select(l =>
                    new GrnLineItemPayload(l.ProductCode, l.ProductNameEn, l.ProductNameZht,
                        l.ExpectedQty, l.UnitOfMeasure)).ToList());
            var result = await client.CreateAsync(payload);
            return result.IsSuccess
                ? Results.Created($"/goods-receipts/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IGoodsReceiptModuleClient client) =>
        {
            var result = await client.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapPost("/{id:guid}/discrepancy", async (
            Guid id,
            RecordDiscrepancyRequest req,
            IGoodsReceiptModuleClient client) =>
        {
            var payload = new RecordDiscrepancyPayload(req.LineItemId, req.ReceivedQty, req.DiscrepancyNote);
            var result = await client.RecordDiscrepancyAsync(id, payload);
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
