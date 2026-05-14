# S6.RL — Rate Limiting on MekaPromosBff

**Author:** McManus  
**Sprint:** 6  
**Story:** S6.RL (2 pts)

## Decisions

### Fixed window over sliding window
Fixed window (`FixedWindowRateLimiter`) chosen over sliding window. Simpler implementation, sufficient accuracy for a 60 req/min consumer BFF limit. Sliding window adds memory overhead with no meaningful benefit at this scale.

### GlobalLimiter partitioned by RemoteIpAddress
`PartitionedRateLimiter.Create` partitions by `context.Connection.RemoteIpAddress?.ToString()`, falling back to `"unknown"` when the IP is null (e.g., in-process test clients). This applies the limit uniformly per IP without requiring any named policy on individual endpoints.

### Health endpoints explicitly exempt
`/health`, `/health/ready`, `/health/live` call `.DisableRateLimiting()` on their `RouteHandlerBuilder`. Load balancers and uptime monitors hit health endpoints continuously — exempting them prevents false 429s on infrastructure probes.

### 429 with Retry-After header
`OnRejected` callback sets `StatusCodes.Status429TooManyRequests` and adds a `Retry-After` header (in seconds) when the lease exposes `MetadataName.RetryAfter` metadata. Response body is `"Rate limit exceeded. Retry later."` for human-readable feedback.

### Middleware placement
`app.UseRateLimiter()` is placed after `app.UseAuthentication()` / `app.UseAuthorization()` so authenticated context is available if per-user policies are added in future sprints.
