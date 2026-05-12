namespace M2.SapConnector;

/// <summary>
/// SAP NCo RFC/BAPI stub (ADR-006 fallback).
/// NCo requires native RFC DLLs that are unavailable in CI/CD environments.
/// Use <see cref="SapODataClient"/> as the primary integration channel.
/// </summary>
public sealed class SapNcoClient : ISapNcoClient
{
    /// <summary>Always throws — the SAP NCo library requires native DLLs not present in this environment.</summary>
    public Task<IReadOnlyList<SapProduct>> GetProductsAsync(
        Guid tenantId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "SAP NCo requires native RFC DLLs — configure SapODataClient as primary");

    // ReSharper disable once UnusedMember.Global
    public void ThrowIfCalled() =>
        throw new NotSupportedException(
            "SAP NCo requires native RFC DLLs — configure SapODataClient as primary");
}
