using M2.Domain.Promotions;

namespace M2.MekaPromosBff.Endpoints;

public static class CouponEndpoints
{
    public static IEndpointRouteBuilder MapCouponEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("Coupons");

        group.MapGet("/members/{memberId:guid}/coupons", async (
            Guid memberId,
            ICouponService svc) =>
        {
            var result = await svc.GetForMemberAsync(memberId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/coupons/{code}/redeem", async (
            string code,
            ICouponService svc) =>
        {
            var result = await svc.RedeemAsync(code);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return app;
    }
}
