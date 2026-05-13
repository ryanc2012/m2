namespace M2.SharedKernel;

/// <summary>
/// Platform-level options for inter-module HTTP communication.
/// Bind from config section "Platform". Override via environment variables in production:
///   Platform__InternalCallSecret, Platform__BaseUrl
/// </summary>
public sealed class InterModuleOptions
{
    /// <summary>
    /// Shared secret for X-Internal-Secret header on intra-platform module calls.
    /// Default "internal" is safe for dev/test only.
    /// </summary>
    public string InternalCallSecret { get; set; } = "internal";

    /// <summary>
    /// Base URL for outbound HTTP calls from BFFs to the Platform API.
    /// Defaults to https://localhost:5100 for local development.
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:5100";

    /// <summary>
    /// API key BFFs send as X-Api-Key header on calls to the Platform API.
    /// Default "platform-dev-key" is safe for dev/test only.
    /// </summary>
    public string ApiKey { get; set; } = "platform-dev-key";
}
