using M2.Domain.Promotions.Dtos;
using M2.Infrastructure.InterModule.Interfaces;

namespace M2.MekaPromosBff.Endpoints;

public static class CouponEndpoints
{
    public static IEndpointRouteBuilder MapCouponEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("Coupons");

        group.MapGet("/members/{memberId:guid}/coupons", async (
            Guid memberId,
            IPromotionsModuleClient client) =>
        {
            var result = await client.GetCouponsByMemberAsync(memberId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/coupons/{code}/redeem", async (
            string code,
            IPromotionsModuleClient client) =>
        {
            var result = await client.RedeemCouponAsync(code);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return app;
    }
}
