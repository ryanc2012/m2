namespace M2.SapConnector;

/// <summary>No-op RFC/BAPI client — stub until NCo library is integrated.</summary>
internal sealed class NoOpSapNcoClient : ISapNcoClient
{
    public Task<IReadOnlyList<SapProduct>> GetProductsAsync(
        Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SapProduct>>(Array.Empty<SapProduct>());
}
