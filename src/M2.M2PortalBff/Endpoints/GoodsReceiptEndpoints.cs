using M2.Domain.Authorization;
using M2.Domain.GoodsReceipt.Dtos;
using M2.Infrastructure.InterModule.Interfaces;
using System.Security.Claims;

namespace M2.M2PortalBff.Endpoints;

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

        group.MapGet("/", async (
            Guid tenantId,
            Guid shopId,
            int page,
            int pageSize,
            IGoodsReceiptModuleClient client) =>
        {
            var result = await client.ListByShopAsync(tenantId, shopId, page, pageSize);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/confirm", async (
            Guid id,
            IAuthorizationService authz,
            ClaimsPrincipal user,
            IGoodsReceiptModuleClient client) =>
        {
            if (await authz.CheckAsync(user, "M_GOODS_RECEIPT_CREATE") == AuthCheckResult.Deny)
                return Results.Forbid();

            var result = await client.ConfirmAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/discrepancy", async (
            Guid id,
            RecordDiscrepancyRequest req,
            IAuthorizationService authz,
            ClaimsPrincipal user,
            IGoodsReceiptModuleClient client) =>
        {
            if (await authz.CheckAsync(user, "M_GOODS_RECEIPT_CREATE") == AuthCheckResult.Deny)
                return Results.Forbid();

            var payload = new RecordDiscrepancyPayload(req.LineItemId, req.ReceivedQty, req.DiscrepancyNote);
            var result = await client.RecordDiscrepancyAsync(id, payload);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/post-to-sap", async (
            Guid id,
            IAuthorizationService authz,
            ClaimsPrincipal user,
            IGoodsReceiptModuleClient client) =>
        {
            if (await authz.CheckAsync(user, "M_GOODS_RECEIPT_CREATE") == AuthCheckResult.Deny)
                return Results.Forbid();

            var result = await client.PostToSapAsync(id);
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
