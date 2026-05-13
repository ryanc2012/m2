using M2.Domain.Members.Dtos;
using M2.Infrastructure.InterModule.Interfaces;

namespace M2.M2PortalBff.Endpoints;

/// <summary>Admin-facing member endpoints on the Portal BFF.</summary>
public static class MemberAdminEndpoints
{
    public static IEndpointRouteBuilder MapMemberAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/members").WithTags("Members");

        group.MapGet("/{id:guid}", async (Guid id, IMembersModuleClient client) =>
        {
            var result = await client.GetByIdAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapGet("/qr/{qrCode}", async (string qrCode, IMembersModuleClient client) =>
        {
            var result = await client.GetByQrCodeAsync(qrCode);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateMemberRequest req,
            IMembersModuleClient client) =>
        {
            var payload = new UpdateMemberProfilePayload(
                req.FirstNameEn, req.FirstNameZht, req.LastNameEn, req.LastNameZht, req.Email);
            var result = await client.UpdateProfileAsync(id, payload);
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
