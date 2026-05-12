using M2.SharedKernel;

namespace M2.Domain.Notifications;

/// <summary>
/// Abstracted SMS gateway (ADR-010). Swap provider (Twilio / AWS SNS / telco) without code changes.
/// </summary>
public interface ISmsGateway
{
    Task<Result> SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default);
}
