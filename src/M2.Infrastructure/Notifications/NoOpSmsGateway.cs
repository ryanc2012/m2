using M2.Domain.Notifications;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Notifications;

/// <summary>
/// No-op SMS gateway stub (ADR-010). Swap in Twilio / AWS SNS / telco without code changes.
/// </summary>
internal sealed class NoOpSmsGateway(ILogger<NoOpSmsGateway> logger) : ISmsGateway
{
    public Task<Result> SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[STUB] SMS intent: to={PhoneNumber} message={Message}",
            phoneNumber, message);

        return Task.FromResult(Result.Success());
    }
}
