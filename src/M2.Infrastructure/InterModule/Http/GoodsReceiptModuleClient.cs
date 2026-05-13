using System.Net.Http.Json;
using M2.Domain.GoodsReceipt.Dtos;
using M2.Infrastructure.InterModule.Interfaces;
using M2.SharedKernel;
using Microsoft.Extensions.Configuration;

namespace M2.Infrastructure.InterModule.Http;

/// <summary>
/// HTTP client for the GoodsReceipt module's /modules/goods-receipt/* endpoints.
/// </summary>
public sealed class GoodsReceiptModuleClient : IGoodsReceiptModuleClient
{
    private const string InternalCallHeader = "X-Internal-Call";
    private const string InternalSecretHeader = "X-Internal-Secret";

    private readonly HttpClient _httpClient;
    private readonly string _secret;

    public GoodsReceiptModuleClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _secret = configuration["Platform:InternalCallSecret"] ?? "internal";
    }

    public async Task<Result<GoodsReceiptNoteDto>> CreateAsync(CreateGoodsReceiptPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, string.Empty, payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<GoodsReceiptNoteDto>(response);
        }
        catch (Exception ex) { return Result.Failure<GoodsReceiptNoteDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<GoodsReceiptNoteDto>> ConfirmAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"{id}/confirm");
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<GoodsReceiptNoteDto>(response);
        }
        catch (Exception ex) { return Result.Failure<GoodsReceiptNoteDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<GoodsReceiptNoteDto>> RecordDiscrepancyAsync(Guid id, RecordDiscrepancyPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"{id}/discrepancy", payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<GoodsReceiptNoteDto>(response);
        }
        catch (Exception ex) { return Result.Failure<GoodsReceiptNoteDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<GoodsReceiptNoteDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, id.ToString());
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<GoodsReceiptNoteDto>(response);
        }
        catch (Exception ex) { return Result.Failure<GoodsReceiptNoteDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<IReadOnlyList<GoodsReceiptNoteDto>>> ListByShopAsync(
        Guid tenantId, Guid shopId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var url = $"?tenantId={tenantId}&shopId={shopId}&page={page}&pageSize={pageSize}";
            using var req = BuildRequest(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<IReadOnlyList<GoodsReceiptNoteDto>>($"GoodsReceipt module returned {(int)response.StatusCode}");
            var list = await response.Content.ReadFromJsonAsync<List<GoodsReceiptNoteDto>>();
            return list is not null
                ? Result.Success<IReadOnlyList<GoodsReceiptNoteDto>>(list.AsReadOnly())
                : Result.Failure<IReadOnlyList<GoodsReceiptNoteDto>>("Empty response from goods-receipt module");
        }
        catch (Exception ex) { return Result.Failure<IReadOnlyList<GoodsReceiptNoteDto>>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result> PostToSapAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"{id}/post-to-sap");
            var response = await _httpClient.SendAsync(req, ct);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure($"GoodsReceipt module returned {(int)response.StatusCode}");
        }
        catch (Exception ex) { return Result.Failure($"Inter-module call failed: {ex.Message}"); }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string relativeUrl, object? body = null)
    {
        var msg = new HttpRequestMessage(method, relativeUrl);
        msg.Headers.Add(InternalCallHeader, "true");
        msg.Headers.Add(InternalSecretHeader, _secret);
        if (body is not null)
            msg.Content = JsonContent.Create(body);
        return msg;
    }

    private static async Task<Result<T>> ReadDto<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            return Result.Failure<T>($"GoodsReceipt module returned {(int)response.StatusCode}");
        var dto = await response.Content.ReadFromJsonAsync<T>();
        return dto is not null ? Result.Success(dto) : Result.Failure<T>("Empty response from goods-receipt module");
    }
}
