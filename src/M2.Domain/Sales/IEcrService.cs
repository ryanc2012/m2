namespace M2.Domain.Sales;

/// <summary>
/// Interface stub for ECR (Electronic Cash Register) integration.
/// ECR integration is deferred post-MVP (ADR-009). No implementation provided.
/// </summary>
public interface IEcrService
{
    /// <summary>Posts a completed sales transaction to the ECR device.</summary>
    Task PostTransactionAsync(Guid transactionId, CancellationToken ct = default);

    /// <summary>Requests a receipt reprint from the ECR device.</summary>
    Task ReprintReceiptAsync(Guid transactionId, CancellationToken ct = default);
}
