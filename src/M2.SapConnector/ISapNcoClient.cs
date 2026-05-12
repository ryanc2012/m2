namespace M2.SapConnector;

/// <summary>
/// SAP NCo RFC/BAPI fallback interface (ADR-006).
/// Methods throw <see cref="NotSupportedException"/> in environments without native NCo DLLs.
/// </summary>
public interface ISapNcoClient
{
    /// <summary>Retrieves product master data via RFC — fallback when OData is unavailable.</summary>
    Task<IReadOnlyList<SapProduct>> GetProductsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
