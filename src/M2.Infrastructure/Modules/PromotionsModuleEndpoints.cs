using M2.Domain.Promotions;
using M2.Domain.Promotions.Dtos;
using M2.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace M2.Infrastructure.Modules;

public static class PromotionsModuleEndpoints
{
    public static IEndpointRouteBuilder MapPromotionsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/modules/promotions")
            .RequireAuthorization()
            .WithTags("Modules.Promotions");

        group.MapPost("/promotions", async (CreatePromotionPayload payload, IPromotionService svc) =>
        {
            if (!Enum.TryParse<PromotionType>(payload.Type, out var type))
                return Results.BadRequest("Invalid promotion type");
            var name = BilingualText.From(payload.NameEn, payload.NameZht);
            var result = await svc.CreateAsync(
                payload.TenantId, payload.ShopId, name, type,
                payload.FormulaJson, payload.StartDate, payload.EndDate, payload.IsStackable);
            return result.IsSuccess ? Results.Ok(ToPromotionDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapGet("/promotions/{id:guid}", async (Guid id, IPromotionService svc) =>
        {
            var result = await svc.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(ToPromotionDto(result.Value!)) : Results.NotFound(result.Error);
        });

        group.MapPost("/promotions/{id:guid}/activate", async (Guid id, IPromotionService svc) =>
        {
            var result = await svc.ActivateAsync(id);
            return result.IsSuccess ? Results.Ok(ToPromotionDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapPost("/promotions/{id:guid}/pause", async (Guid id, IPromotionService svc) =>
        {
            var result = await svc.PauseAsync(id);
            return result.IsSuccess ? Results.Ok(ToPromotionDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapGet("/promotions/active", async (Guid tenantId, Guid shopId, IPromotionService svc) =>
        {
            var result = await svc.GetActiveForShopAsync(tenantId, shopId);
            if (!result.IsSuccess) return Results.BadRequest(result.Error);
            return Results.Ok(result.Value!.Select(ToPromotionDto).ToList());
        });

        group.MapGet("/coupons", async (Guid memberId, ICouponService svc) =>
        {
            var result = await svc.GetForMemberAsync(memberId);
            if (!result.IsSuccess) return Results.BadRequest(result.Error);
            return Results.Ok(result.Value!.Select(ToCouponDto).ToList());
        });

        group.MapPost("/coupons/{code}/redeem", async (string code, ICouponService svc) =>
        {
            var result = await svc.RedeemAsync(code);
            return result.IsSuccess ? Results.Ok(ToCouponDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapPost("/discounts/calculate", async (CalculateDiscountPayload payload, IDiscountEngine engine) =>
        {
            var cartItems = payload.CartItems.Select(i => new CartItem(i.ProductId, i.Quantity, i.UnitPrice));
            var result = await engine.CalculateAsync(cartItems, payload.MemberId, payload.ShopId);
            return result.IsSuccess ? Results.Ok(result.Value!) : Results.BadRequest(result.Error);
        });

        return app;
    }

    private static PromotionDto ToPromotionDto(Promotion p) => new(
        p.Id, p.TenantId, p.ShopId,
        p.Name.En, p.Name.Zht,
        p.Type.ToString(), p.Status.ToString(),
        p.FormulaJson, p.StartDate, p.EndDate, p.IsStackable, p.ApprovalRequestId);

    private static CouponDto ToCouponDto(Coupon c) => new(
        c.Id, c.TenantId, c.ShopId,
        c.PromotionId, c.MemberId, c.Code,
        c.IssuedAt, c.RedeemedAt, c.ExpiresAt, c.IsRedeemed);
}
