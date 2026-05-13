using System.Net.Http.Json;
using M2.Domain.Sales.Dtos;
using M2.Infrastructure.InterModule.Interfaces;
using M2.SharedKernel;
using Microsoft.Extensions.Configuration;

namespace M2.Infrastructure.InterModule.Http;

/// <summary>
/// HTTP client for the Sales module's /modules/sales/* endpoints.
/// </summary>
public sealed class SalesModuleClient : ISalesModuleClient
{
    private const string InternalCallHeader = "X-Internal-Call";
    private const string InternalSecretHeader = "X-Internal-Secret";

    private readonly HttpClient _httpClient;
    private readonly string _secret;

    public SalesModuleClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _secret = configuration["Platform:InternalCallSecret"] ?? "internal";
    }

    public async Task<Result<SalesTransactionDto>> CreateTransactionAsync(CreateTransactionPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, "transactions", payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<SalesTransactionDto>(response);
        }
        catch (Exception ex) { return Result.Failure<SalesTransactionDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<SalesTransactionDto>> CompleteTransactionAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"transactions/{id}/complete");
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<SalesTransactionDto>(response);
        }
        catch (Exception ex) { return Result.Failure<SalesTransactionDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<SalesTransactionDto>> VoidTransactionAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"transactions/{id}/void");
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<SalesTransactionDto>(response);
        }
        catch (Exception ex) { return Result.Failure<SalesTransactionDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<SalesTransactionDto>> GetTransactionByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, $"transactions/{id}");
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<SalesTransactionDto>(response);
        }
        catch (Exception ex) { return Result.Failure<SalesTransactionDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<ReturnTransactionDto>> InitiateReturnAsync(InitiateReturnPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, "returns", payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<ReturnTransactionDto>(response);
        }
        catch (Exception ex) { return Result.Failure<ReturnTransactionDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<ReturnTransactionDto>> CompleteReturnAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"returns/{id}/complete");
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<ReturnTransactionDto>(response);
        }
        catch (Exception ex) { return Result.Failure<ReturnTransactionDto>($"Inter-module call failed: {ex.Message}"); }
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
            return Result.Failure<T>($"Sales module returned {(int)response.StatusCode}");
        var dto = await response.Content.ReadFromJsonAsync<T>();
        return dto is not null ? Result.Success(dto) : Result.Failure<T>("Empty response from sales module");
    }
}
