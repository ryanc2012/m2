namespace M2.SharedKernel;

/// <summary>
/// Platform-level options for inter-module HTTP communication.
/// Bind from config section "Platform". Override via environment variables in production:
///   Platform__InternalCallSecret, Platform__BaseUrl
/// </summary>
public sealed class InterModuleOptions
{
    /// <summary>
    /// Shared secret for X-Internal-Secret header on inter-module calls.
    /// Default "internal" is safe for dev/test only.
    /// </summary>
    public string InternalCallSecret { get; set; } = "internal";

    /// <summary>
    /// Base URL for outbound inter-module HTTP calls.
    /// Defaults to https://localhost:5000 for local development.
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:5000";
}
