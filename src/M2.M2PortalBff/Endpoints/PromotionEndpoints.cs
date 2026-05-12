using M2.Domain.Promotions;
using M2.SharedKernel;

namespace M2.M2PortalBff.Endpoints;

public static class PromotionEndpoints
{
    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/promotions").WithTags("Promotions");

        group.MapPost("/", async (
            CreatePromotionRequest req,
            IPromotionService svc) =>
        {
            var name = new BilingualText(req.NameEn, req.NameZht);
            var result = await svc.CreateAsync(
                req.TenantId, req.ShopId, name, req.Type,
                req.FormulaJson, req.StartDate, req.EndDate, req.IsStackable);
            return result.IsSuccess
                ? Results.Created($"/promotions/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}", async (Guid id, IPromotionService svc) =>
        {
            var result = await svc.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, IPromotionService svc) =>
        {
            var result = await svc.ActivateAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/pause", async (Guid id, IPromotionService svc) =>
        {
            var result = await svc.PauseAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/active", async (
            Guid tenantId,
            Guid shopId,
            IPromotionService svc) =>
        {
            var result = await svc.GetActiveForShopAsync(tenantId, shopId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return app;
    }
}

public record CreatePromotionRequest(
    Guid TenantId,
    Guid ShopId,
    string NameEn,
    string NameZht,
    PromotionType Type,
    string FormulaJson,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool IsStackable);
