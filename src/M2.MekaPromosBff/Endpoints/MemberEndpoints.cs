using M2.Domain.Members;
using M2.SharedKernel;

namespace M2.MekaPromosBff.Endpoints;

/// <summary>Consumer-facing member endpoints on the MekaPromos BFF.</summary>
public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/members").WithTags("Members");

        group.MapPost("/register", async (
            RegisterMemberRequest req,
            IMemberService svc) =>
        {
            var firstName = BilingualText.From(req.FirstNameEn, req.FirstNameZht);
            var lastName = BilingualText.From(req.LastNameEn, req.LastNameZht);

            var result = await svc.RegisterAsync(
                req.TenantId, req.ShopId,
                firstName, lastName,
                req.Phone, req.Email, req.MembershipTier);

            return result.IsSuccess
                ? Results.Created($"/members/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMemberService svc) =>
        {
            var result = await svc.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapGet("/qr/{qrCode}", async (string qrCode, IMemberService svc) =>
        {
            var result = await svc.GetByQrCodeAsync(qrCode);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapPost("/{id:guid}/otp/generate", async (Guid id, IOtpService svc) =>
        {
            var result = await svc.GenerateAsync(id);
            return result.IsSuccess ? Results.Ok(new { result.Value!.ExpiresAt }) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/otp/validate", async (
            Guid id,
            ValidateOtpRequest req,
            IOtpService svc) =>
        {
            var result = await svc.ValidateAsync(id, req.Code);
            return result.IsSuccess
                ? Results.Ok(new { Valid = result.Value })
                : Results.BadRequest(result.Error);
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateMemberRequest req,
            IMemberService svc) =>
        {
            var firstName = BilingualText.From(req.FirstNameEn, req.FirstNameZht);
            var lastName = BilingualText.From(req.LastNameEn, req.LastNameZht);
            var result = await svc.UpdateProfileAsync(id, firstName, lastName, req.Email);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return app;
    }
}

public record RegisterMemberRequest(
    Guid TenantId,
    Guid ShopId,
    string FirstNameEn,
    string FirstNameZht,
    string LastNameEn,
    string LastNameZht,
    string Phone,
    string? Email,
    MembershipTier MembershipTier);

public record ValidateOtpRequest(string Code);

public record UpdateMemberRequest(
    string FirstNameEn,
    string FirstNameZht,
    string LastNameEn,
    string LastNameZht,
    string? Email);
