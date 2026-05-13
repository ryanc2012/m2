using M2.Domain.Promotions;
using M2.Domain.Promotions.Dtos;
using M2.Infrastructure.InterModule.Interfaces;

namespace M2.M2PortalBff.Endpoints;

public static class PromotionEndpoints
{
    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/promotions").WithTags("Promotions");

        group.MapPost("/", async (
            CreatePromotionRequest req,
            IPromotionsModuleClient client) =>
        {
            var payload = new CreatePromotionPayload(
                req.TenantId, req.ShopId, req.NameEn, req.NameZht,
                req.Type.ToString(), req.FormulaJson, req.StartDate, req.EndDate, req.IsStackable);
            var result = await client.CreatePromotionAsync(payload);
            return result.IsSuccess
                ? Results.Created($"/promotions/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}", async (Guid id, IPromotionsModuleClient client) =>
        {
            var result = await client.GetPromotionByIdAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, IPromotionsModuleClient client) =>
        {
            var result = await client.ActivatePromotionAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/pause", async (Guid id, IPromotionsModuleClient client) =>
        {
            var result = await client.PausePromotionAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/active", async (
            Guid tenantId,
            Guid shopId,
            IPromotionsModuleClient client) =>
        {
            var result = await client.GetActivePromotionsForShopAsync(tenantId, shopId);
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
