using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace M2.SharedKernel;

public static class SharedKernelServiceExtensions
{
    /// <summary>
    /// Registers inter-module auth options from the "Platform" config section.
    /// Typed clients use <see cref="InterModuleOptions"/> (via IOptions) to read
    /// X-Internal-Secret for outbound calls.
    /// In production, override via env vars: Platform__InternalCallSecret, Platform__BaseUrl.
    /// </summary>
    public static IServiceCollection AddInterModuleAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<InterModuleOptions>(configuration.GetSection("Platform"));
        return services;
    }
}
