using Microsoft.AspNetCore.Http;

namespace M2.SharedKernel.Middleware;

/// <summary>
/// Validates X-Api-Key header for service-to-service calls (BE-REC-001 R4).
/// SHA-256 hashed keys; plaintext never persisted.
/// </summary>
public sealed class ApiKeyMiddleware(RequestDelegate next)
{
    // TODO Sprint 2: load valid key hashes from config; implement SHA-256 comparison
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);
    }
}
