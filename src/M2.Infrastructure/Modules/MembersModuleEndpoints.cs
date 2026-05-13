using M2.Domain.Members;
using M2.Domain.Members.Dtos;
using M2.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace M2.Infrastructure.Modules;

public static class MembersModuleEndpoints
{
    public static IEndpointRouteBuilder MapMembersModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/modules/members")
            .RequireAuthorization()
            .WithTags("Modules.Members");

        group.MapPost("/register", async (RegisterMemberPayload payload, IMemberService svc) =>
        {
            var firstName = BilingualText.From(payload.FirstNameEn, payload.FirstNameZht);
            var lastName = BilingualText.From(payload.LastNameEn, payload.LastNameZht);
            if (!Enum.TryParse<MembershipTier>(payload.MembershipTier, out var tier))
                return Results.BadRequest("Invalid membership tier");
            var result = await svc.RegisterAsync(
                payload.TenantId, payload.ShopId, firstName, lastName, payload.Phone, payload.Email, tier);
            return result.IsSuccess
                ? Results.Ok(ToDto(result.Value!))
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMemberService svc) =>
        {
            var result = await svc.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.NotFound(result.Error);
        });

        group.MapGet("/qr/{qrCode}", async (string qrCode, IMemberService svc) =>
        {
            var result = await svc.GetByQrCodeAsync(qrCode);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.NotFound(result.Error);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateMemberProfilePayload payload, IMemberService svc) =>
        {
            var firstName = BilingualText.From(payload.FirstNameEn, payload.FirstNameZht);
            var lastName = BilingualText.From(payload.LastNameEn, payload.LastNameZht);
            var result = await svc.UpdateProfileAsync(id, firstName, lastName, payload.Email);
            return result.IsSuccess ? Results.Ok(ToDto(result.Value!)) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IMemberService svc) =>
        {
            var result = await svc.DeactivateAsync(id);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        return app;
    }

    private static MemberDto ToDto(Member m) => new(
        m.Id, m.TenantId, m.ShopId,
        m.FirstName.En, m.FirstName.Zht,
        m.LastName.En, m.LastName.Zht,
        m.Phone, m.Email, m.QrCode,
        m.MembershipTier.ToString(), m.JoinedAt, m.IsActive);
}
