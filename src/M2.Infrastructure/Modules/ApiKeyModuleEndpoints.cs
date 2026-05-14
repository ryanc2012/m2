using M2.Domain.ApiKeys;
using M2.Domain.Authorization;
using M2.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace M2.Infrastructure.Modules;

public static class ApiKeyModuleEndpoints
{
    public static IEndpointRouteBuilder MapApiKeyModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/apikeys")
            .WithTags("ApiKeys")
            .RequireAuthorization();

        group.MapPost("/", async (
            CreateApiKeyRequest req,
            IApiKeyService svc,
            ClaimsPrincipal user,
            IAuthorizationService authz) =>
        {
            if (await authz.CheckAsync(user, "M_APIKEY_MANAGE") == AuthCheckResult.Deny)
                return Results.Forbid();

            var (key, plaintext) = await svc.CreateAsync(
                WellKnownTenants.Default,
                req.Name,
                req.Scopes,
                user.Identity!.Name ?? "system",
                req.ExpiresAt,
                default);

            return Results.Created($"/api/v1/apikeys/{key.Id}", new { key, plaintext });
        });

        group.MapGet("/", async (
            IApiKeyService svc,
            ClaimsPrincipal user,
            IAuthorizationService authz) =>
        {
            if (await authz.CheckAsync(user, "M_APIKEY_MANAGE") == AuthCheckResult.Deny)
                return Results.Forbid();

            var keys = await svc.ListAsync(WellKnownTenants.Default, default);
            return Results.Ok(keys);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IApiKeyService svc,
            ClaimsPrincipal user,
            IAuthorizationService authz) =>
        {
            if (await authz.CheckAsync(user, "M_APIKEY_MANAGE") == AuthCheckResult.Deny)
                return Results.Forbid();

            var deleted = await svc.RevokeAsync(WellKnownTenants.Default, id, default);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}

public record CreateApiKeyRequest(string Name, string[] Scopes, DateTimeOffset? ExpiresAt);
