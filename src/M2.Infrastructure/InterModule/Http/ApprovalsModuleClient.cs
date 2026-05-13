using System.Net.Http.Json;
using M2.Domain.Approvals.Dtos;
using M2.Infrastructure.InterModule.Interfaces;
using M2.SharedKernel;
using Microsoft.Extensions.Configuration;

namespace M2.Infrastructure.InterModule.Http;

/// <summary>
/// HTTP client for the Approvals module's /modules/approvals/* endpoints.
/// </summary>
public sealed class ApprovalsModuleClient : IApprovalsModuleClient
{
    private const string InternalCallHeader = "X-Internal-Call";
    private const string InternalSecretHeader = "X-Internal-Secret";

    private readonly HttpClient _httpClient;
    private readonly string _secret;

    public ApprovalsModuleClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _secret = configuration["Platform:InternalCallSecret"] ?? "internal";
    }

    public async Task<Result<ApprovalRequestDto>> CreateRequestAsync(CreateApprovalPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, "requests", payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<ApprovalRequestDto>(response);
        }
        catch (Exception ex) { return Result.Failure<ApprovalRequestDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<ApprovalRequestDto>> GetRequestAsync(Guid requestId, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, $"requests/{requestId}");
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<ApprovalRequestDto>(response);
        }
        catch (Exception ex) { return Result.Failure<ApprovalRequestDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<IReadOnlyList<ApprovalRequestDto>>> GetPendingRequestsForApproverAsync(string approverId, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, $"pending?approverId={Uri.EscapeDataString(approverId)}");
            var response = await _httpClient.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<IReadOnlyList<ApprovalRequestDto>>($"Approvals module returned {(int)response.StatusCode}");
            var list = await response.Content.ReadFromJsonAsync<List<ApprovalRequestDto>>();
            return list is not null
                ? Result.Success<IReadOnlyList<ApprovalRequestDto>>(list.AsReadOnly())
                : Result.Failure<IReadOnlyList<ApprovalRequestDto>>("Empty response from approvals module");
        }
        catch (Exception ex) { return Result.Failure<IReadOnlyList<ApprovalRequestDto>>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<ApprovalRequestDto>> ApproveStepAsync(Guid requestId, ApproveRejectPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"requests/{requestId}/approve", payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<ApprovalRequestDto>(response);
        }
        catch (Exception ex) { return Result.Failure<ApprovalRequestDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<ApprovalRequestDto>> RejectStepAsync(Guid requestId, ApproveRejectPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"requests/{requestId}/reject", payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<ApprovalRequestDto>(response);
        }
        catch (Exception ex) { return Result.Failure<ApprovalRequestDto>($"Inter-module call failed: {ex.Message}"); }
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
            return Result.Failure<T>($"Approvals module returned {(int)response.StatusCode}");
        var dto = await response.Content.ReadFromJsonAsync<T>();
        return dto is not null ? Result.Success(dto) : Result.Failure<T>("Empty response from approvals module");
    }
}
