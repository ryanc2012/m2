using M2.Domain.Sales;
using M2.Domain.Sales.Dtos;
using M2.Infrastructure.InterModule.Interfaces;

namespace M2.MekaPosBff.Endpoints;

public static class SalesEndpoints
{
    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sales").WithTags("Sales");

        group.MapPost("/transactions", async (
            CreateTransactionRequest req,
            ISalesModuleClient client) =>
        {
            var payload = new CreateTransactionPayload(
                req.TenantId, req.ShopId, req.MemberId, req.CashierId,
                req.PaymentMethod.ToString(),
                req.LineItems.Select(l => new LineItemPayload(
                    l.ProductId, l.ProductNameEn, l.ProductNameZht,
                    l.Quantity, l.UnitPrice, l.DiscountAmount)).ToList());
            var result = await client.CreateTransactionAsync(payload);
            return result.IsSuccess
                ? Results.Created($"/sales/transactions/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapPost("/transactions/{id:guid}/complete", async (
            Guid id,
            ISalesModuleClient client) =>
        {
            var result = await client.CompleteTransactionAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/transactions/{id:guid}/void", async (
            Guid id,
            ISalesModuleClient client) =>
        {
            var result = await client.VoidTransactionAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/transactions/{id:guid}", async (
            Guid id,
            ISalesModuleClient client) =>
        {
            var result = await client.GetTransactionByIdAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        var returnGroup = app.MapGroup("/sales/returns").WithTags("Returns");

        returnGroup.MapPost("/", async (
            InitiateReturnRequest req,
            ISalesModuleClient client) =>
        {
            var payload = new InitiateReturnPayload(
                req.TenantId, req.ShopId, req.OriginalTransactionId, req.Reason, req.RefundAmount);
            var result = await client.InitiateReturnAsync(payload);
            return result.IsSuccess
                ? Results.Created($"/sales/returns/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        returnGroup.MapPost("/{id:guid}/complete", async (
            Guid id,
            ISalesModuleClient client) =>
        {
            var result = await client.CompleteReturnAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return app;
    }
}

public record CreateTransactionRequest(
    Guid TenantId,
    Guid ShopId,
    Guid? MemberId,
    string CashierId,
    PaymentMethod PaymentMethod,
    List<LineItemDto> LineItems);

public record LineItemDto(
    string ProductId,
    string ProductNameEn,
    string ProductNameZht,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount = 0);

public record InitiateReturnRequest(
    Guid TenantId,
    Guid ShopId,
    Guid OriginalTransactionId,
    string Reason,
    decimal RefundAmount);
