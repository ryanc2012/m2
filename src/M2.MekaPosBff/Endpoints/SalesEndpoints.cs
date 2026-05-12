using M2.Domain.Sales;

namespace M2.MekaPosBff.Endpoints;

public static class SalesEndpoints
{
    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sales").WithTags("Sales");

        group.MapPost("/transactions", async (
            CreateTransactionRequest req,
            ISalesService svc) =>
        {
            var lineItems = req.LineItems.Select(l =>
                new LineItemRequest(l.ProductId, l.ProductNameEn, l.ProductNameZht,
                    l.Quantity, l.UnitPrice, l.DiscountAmount));

            var result = await svc.CreateTransactionAsync(
                req.TenantId, req.ShopId, req.MemberId, req.CashierId, req.PaymentMethod, lineItems);
            return result.IsSuccess
                ? Results.Created($"/sales/transactions/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapPost("/transactions/{id:guid}/complete", async (
            Guid id,
            ISalesService svc) =>
        {
            var result = await svc.CompleteTransactionAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/transactions/{id:guid}/void", async (
            Guid id,
            ISalesService svc) =>
        {
            var result = await svc.VoidTransactionAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/transactions/{id:guid}", async (
            Guid id,
            ISalesService svc) =>
        {
            var result = await svc.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        var returnGroup = app.MapGroup("/sales/returns").WithTags("Returns");

        returnGroup.MapPost("/", async (
            InitiateReturnRequest req,
            IReturnService svc) =>
        {
            var result = await svc.InitiateReturnAsync(
                req.TenantId, req.ShopId, req.OriginalTransactionId, req.Reason, req.RefundAmount);
            return result.IsSuccess
                ? Results.Created($"/sales/returns/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        returnGroup.MapPost("/{id:guid}/complete", async (
            Guid id,
            IReturnService svc) =>
        {
            var result = await svc.CompleteReturnAsync(id);
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
