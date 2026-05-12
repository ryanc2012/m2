using M2.Domain.Members;
using M2.SharedKernel;

namespace M2.M2PortalBff.Endpoints;

/// <summary>Admin-facing member endpoints on the Portal BFF.</summary>
public static class MemberAdminEndpoints
{
    public static IEndpointRouteBuilder MapMemberAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/members").WithTags("Members");

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

public record UpdateMemberRequest(
    string FirstNameEn,
    string FirstNameZht,
    string LastNameEn,
    string LastNameZht,
    string? Email);
