using M2.SharedKernel;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace M2.SapConnector;

/// <summary>
/// Concrete SAP OData REST client (ADR-006 primary channel).
/// Base URL configured via <c>Sap:ODataBaseUrl</c> in IConfiguration.
/// All calls wrapped in try/catch — failures return Result.Failure (never throw).
/// </summary>
public sealed class SapODataClient : ISapODataClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public SapODataClient(HttpClient httpClient, IConfiguration configuration)
    {
        _http = httpClient;
        _baseUrl = configuration["Sap:ODataBaseUrl"] ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(_baseUrl))
            _http.BaseAddress = new Uri(_baseUrl.TrimEnd('/') + "/");
    }

    public async Task<Result<IReadOnlyList<SapProduct>>> GetProductsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<SapODataList<SapProduct>>(
                $"Products?tenantId={tenantId}", cancellationToken);
            return Result.Success<IReadOnlyList<SapProduct>>(
                response?.Value ?? Array.Empty<SapProduct>());
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<SapProduct>>(
                $"SAP GetProductsAsync failed: {ex.Message}");
        }
    }

    public async Task<Result<SapOrganisation>> GetOrganisationAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<SapOrganisation>(
                $"OrganisationUnits?tenantId={tenantId}", cancellationToken);
            return response is not null
                ? Result.Success(response)
                : Result.Failure<SapOrganisation>("SAP returned no organisation data.");
        }
        catch (Exception ex)
        {
            return Result.Failure<SapOrganisation>(
                $"SAP GetOrganisationAsync failed: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<SapEmployee>>> GetEmployeesAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<SapODataList<SapEmployee>>(
                $"Employees?tenantId={tenantId}", cancellationToken);
            return Result.Success<IReadOnlyList<SapEmployee>>(
                response?.Value ?? Array.Empty<SapEmployee>());
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<SapEmployee>>(
                $"SAP GetEmployeesAsync failed: {ex.Message}");
        }
    }

    public async Task<Result> PostGoodsMovementAsync(
        Guid tenantId, SapGoodsMovementPayload payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                $"GoodsMovements?tenantId={tenantId}", payload, cancellationToken);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure($"SAP PostGoodsMovementAsync failed: HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"SAP PostGoodsMovementAsync failed: {ex.Message}");
        }
    }

    /// <summary>Thin wrapper to deserialise OData collection responses.</summary>
    private sealed class SapODataList<T>
    {
        public IReadOnlyList<T> Value { get; init; } = Array.Empty<T>();
    }
}
