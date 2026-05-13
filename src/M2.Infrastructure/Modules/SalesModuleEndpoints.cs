using M2.Domain.Sales;
using M2.Domain.Sales.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace M2.Infrastructure.Modules;

public static class SalesModuleEndpoints
{
    public static IEndpointRouteBuilder MapSalesModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/modules/sales")
            .RequireAuthorization()
            .WithTags("Modules.Sales");

        group.MapPost("/transactions", async (CreateTransactionPayload payload, ISalesService svc) =>
        {
            if (!Enum.TryParse<PaymentMethod>(payload.PaymentMethod, out var pm))
                return Results.BadRequest("Invalid payment method");
            var lineItems = payload.LineItems.Select(l =>
                new LineItemRequest(l.ProductId, l.ProductNameEn, l.ProductNameZht, l.Quantity, l.UnitPrice, l.DiscountAmount));
            var result = await svc.CreateTransactionAsync(
                payload.TenantId, payload.ShopId, payload.MemberId, payload.CashierId, pm, lineItems);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapPost("/transactions/{id:guid}/complete", async (Guid id, ISalesService svc) =>
        {
            var result = await svc.CompleteTransactionAsync(id);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapPost("/transactions/{id:guid}/void", async (Guid id, ISalesService svc) =>
        {
            var result = await svc.VoidTransactionAsync(id);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapGet("/transactions/{id:guid}", async (Guid id, ISalesService svc) =>
        {
            var result = await svc.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.NotFound(result.Error);
        });

        group.MapPost("/returns", async (InitiateReturnPayload payload, IReturnService svc) =>
        {
            var result = await svc.InitiateReturnAsync(
                payload.TenantId, payload.ShopId, payload.OriginalTransactionId, payload.Reason, payload.RefundAmount);
            return result.IsSuccess ? Results.Ok(ToReturnDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapPost("/returns/{id:guid}/complete", async (Guid id, IReturnService svc) =>
        {
            var result = await svc.CompleteReturnAsync(id);
            return result.IsSuccess ? Results.Ok(ToReturnDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        return app;
    }

    private static SalesTransactionDto ToDto(SalesTransaction t) => new(
        t.Id, t.TenantId, t.ShopId,
        t.MemberId, t.CashierId,
        t.TotalAmount, t.DiscountAmount,
        t.PaymentMethod.ToString(), t.Status.ToString(),
        t.CreatedAt, t.CompletedAt, t.VoidedAt);

    private static ReturnTransactionDto ToReturnDto(ReturnTransaction r) => new(
        r.Id, r.TenantId, r.ShopId,
        r.OriginalTransactionId, r.Reason, r.RefundAmount,
        r.RefundMethod.ToString(),
        r.CreatedAt, r.ProcessedAt, r.IsComplete);
}
