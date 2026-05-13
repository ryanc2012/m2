using System.Net.Http.Json;
using M2.Domain.Promotions;
using M2.Domain.Promotions.Dtos;
using M2.Infrastructure.InterModule.Interfaces;
using M2.SharedKernel;
using Microsoft.Extensions.Configuration;

namespace M2.Infrastructure.InterModule.Http;

/// <summary>
/// HTTP client for the Promotions module's /modules/promotions/* endpoints.
/// </summary>
public sealed class PromotionsModuleClient : IPromotionsModuleClient
{
    private const string InternalCallHeader = "X-Internal-Call";
    private const string InternalSecretHeader = "X-Internal-Secret";

    private readonly HttpClient _httpClient;
    private readonly string _secret;

    public PromotionsModuleClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _secret = configuration["Platform:InternalCallSecret"] ?? "internal";
    }

    public async Task<Result<PromotionDto>> CreatePromotionAsync(CreatePromotionPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, "promotions", payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<PromotionDto>(response);
        }
        catch (Exception ex) { return Result.Failure<PromotionDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<PromotionDto>> GetPromotionByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, $"promotions/{id}");
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<PromotionDto>(response);
        }
        catch (Exception ex) { return Result.Failure<PromotionDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<PromotionDto>> ActivatePromotionAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"promotions/{id}/activate");
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<PromotionDto>(response);
        }
        catch (Exception ex) { return Result.Failure<PromotionDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<PromotionDto>> PausePromotionAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"promotions/{id}/pause");
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<PromotionDto>(response);
        }
        catch (Exception ex) { return Result.Failure<PromotionDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<IReadOnlyList<PromotionDto>>> GetActivePromotionsForShopAsync(Guid tenantId, Guid shopId, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, $"promotions/active?tenantId={tenantId}&shopId={shopId}");
            var response = await _httpClient.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<IReadOnlyList<PromotionDto>>($"Promotions module returned {(int)response.StatusCode}");
            var list = await response.Content.ReadFromJsonAsync<List<PromotionDto>>();
            return list is not null
                ? Result.Success<IReadOnlyList<PromotionDto>>(list.AsReadOnly())
                : Result.Failure<IReadOnlyList<PromotionDto>>("Empty response from promotions module");
        }
        catch (Exception ex) { return Result.Failure<IReadOnlyList<PromotionDto>>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<IReadOnlyList<CouponDto>>> GetCouponsByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, $"coupons?memberId={memberId}");
            var response = await _httpClient.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<IReadOnlyList<CouponDto>>($"Promotions module returned {(int)response.StatusCode}");
            var list = await response.Content.ReadFromJsonAsync<List<CouponDto>>();
            return list is not null
                ? Result.Success<IReadOnlyList<CouponDto>>(list.AsReadOnly())
                : Result.Failure<IReadOnlyList<CouponDto>>("Empty response from promotions module");
        }
        catch (Exception ex) { return Result.Failure<IReadOnlyList<CouponDto>>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<CouponDto>> RedeemCouponAsync(string code, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"coupons/{Uri.EscapeDataString(code)}/redeem");
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<CouponDto>(response);
        }
        catch (Exception ex) { return Result.Failure<CouponDto>($"Inter-module call failed: {ex.Message}"); }
    }

    public async Task<Result<DiscountResult>> CalculateDiscountsAsync(CalculateDiscountPayload payload, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, "discounts/calculate", payload);
            var response = await _httpClient.SendAsync(req, ct);
            return await ReadDto<DiscountResult>(response);
        }
        catch (Exception ex) { return Result.Failure<DiscountResult>($"Inter-module call failed: {ex.Message}"); }
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
            return Result.Failure<T>($"Promotions module returned {(int)response.StatusCode}");
        var dto = await response.Content.ReadFromJsonAsync<T>();
        return dto is not null ? Result.Success(dto) : Result.Failure<T>("Empty response from promotions module");
    }
}
