using M2.SharedKernel;

namespace M2.SapConnector;

/// <summary>No-op implementation — replaced by real OData client in Epic 9.</summary>
internal sealed class NoOpSapODataClient : ISapODataClient
{
    public Task<Result<IReadOnlyList<SapProduct>>> GetProductsAsync(
        Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success<IReadOnlyList<SapProduct>>(Array.Empty<SapProduct>()));

    public Task<Result<SapOrganisation>> GetOrganisationAsync(
        Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(new SapOrganisation("0000", BilingualText.Empty)));

    public Task<Result<IReadOnlyList<SapEmployee>>> GetEmployeesAsync(
        Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success<IReadOnlyList<SapEmployee>>(Array.Empty<SapEmployee>()));

    public Task<Result> PostGoodsMovementAsync(
        Guid tenantId, SapGoodsMovementPayload payload, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());
}
