using System.Net.Http.Json;
using M2.Domain.Members.Dtos;
using M2.Infrastructure.InterModule.Interfaces;
using M2.SharedKernel;
using Microsoft.Extensions.Configuration;

namespace M2.Infrastructure.InterModule.Http;

/// <summary>
/// HTTP client for the Members module's /modules/members/* endpoints.
/// Adds X-Internal-Call and X-Internal-Secret headers on every request.
/// </summary>
public sealed class MembersModuleClient : IMembersModuleClient
{
    private const string InternalCallHeader = "X-Internal-Call";
    private const string InternalSecretHeader = "X-Internal-Secret";

    private readonly HttpClient _httpClient;
    private readonly string _secret;

    public MembersModuleClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _secret = configuration["Platform:InternalCallSecret"] ?? "internal";
    }

    public async Task<Result<MemberDto>> RegisterAsync(RegisterMemberPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, "register", payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<MemberDto>(response);
        }
        catch (Exception ex) { return Result.Failure<MemberDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<MemberDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, id.ToString());
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<MemberDto>(response);
        }
        catch (Exception ex) { return Result.Failure<MemberDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<MemberDto>> GetByQrCodeAsync(string qrCode, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, $"qr/{Uri.EscapeDataString(qrCode)}");
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<MemberDto>(response);
        }
        catch (Exception ex) { return Result.Failure<MemberDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<MemberDto>> UpdateProfileAsync(Guid id, UpdateMemberProfilePayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Put, id.ToString(), payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<MemberDto>(response);
        }
        catch (Exception ex) { return Result.Failure<MemberDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"{id}/deactivate");
            var response = await _httpClient.SendAsync(req, ct);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure($"Members module returned {(int)response.StatusCode}");
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
            return Result.Failure<T>($"Members module returned {(int)response.StatusCode}");
        var dto = await response.Content.ReadFromJsonAsync<T>();
        return dto is not null ? Result.Success(dto) : Result.Failure<T>("Empty response from members module");
    }
}
