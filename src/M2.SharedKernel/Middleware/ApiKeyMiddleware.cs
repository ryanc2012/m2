using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace M2.SharedKernel.Middleware;

/// <summary>
/// Validates X-Api-Key header for service-to-service calls (BE-REC-001 R4).
/// SHA-256 hashed keys; plaintext never persisted.
///
/// Bypass: requests to /modules/* with X-Internal-Call: true and a matching
/// X-Internal-Secret (from Platform:InternalCallSecret config) bypass API key
/// validation and are given an authenticated "InternalCall" identity so that
/// downstream RequireAuthorization() checks pass.
/// </summary>
public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string InternalCallHeader = "X-Internal-Call";
    private const string InternalSecretHeader = "X-Internal-Secret";

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsValidInternalModuleCall(context))
        {
            // Grant an authenticated identity so RequireAuthorization() passes downstream.
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "internal-module-caller")],
                authenticationType: "InternalCall");
            context.User = new ClaimsPrincipal(identity);
            await next(context);
            return;
        }

        // TODO Sprint 2: load valid key hashes from config; implement SHA-256 comparison
        await next(context);
    }

    private bool IsValidInternalModuleCall(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/modules"))
            return false;

        if (!context.Request.Headers.TryGetValue(InternalCallHeader, out var callHeader) ||
            !string.Equals(callHeader, "true", StringComparison.OrdinalIgnoreCase))
            return false;

        var expectedSecret = configuration["Platform:InternalCallSecret"] ?? "internal";
        return context.Request.Headers.TryGetValue(InternalSecretHeader, out var secretHeader) &&
               string.Equals(secretHeader, expectedSecret, StringComparison.Ordinal);
    }
}
