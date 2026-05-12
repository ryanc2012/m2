using M2.Domain.Members;
using M2.SharedKernel;
using Microsoft.Extensions.Logging;

namespace M2.Infrastructure.Members;

internal sealed class OtpService(
    IMemberService memberService,
    ILogger<OtpService> logger) : IOtpService
{
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
    private static readonly Dictionary<Guid, OtpRequest> _otps = [];

    public async Task<Result<OtpRequest>> GenerateAsync(Guid memberId, CancellationToken ct = default)
    {
        var memberResult = await memberService.GetByIdAsync(memberId, ct);
        if (memberResult.IsFailure)
            return Result.Failure<OtpRequest>(memberResult.Error!);

        // Invalidate any existing OTP for this member
        var existing = _otps.Values.Where(o => o.MemberId == memberId && o.IsValid()).ToList();
        foreach (var old in existing)
            old.MarkUsed();

        var code = GenerateCode();
        var otp = new OtpRequest(memberId, code, OtpTtl);
        _otps[otp.Id] = otp;

        logger.LogInformation("[STUB] OTP generated for member={MemberId} (SMS dispatch deferred)", memberId);

        return Result.Success(otp);
    }

    public async Task<Result<bool>> ValidateAsync(Guid memberId, string code, CancellationToken ct = default)
    {
        var memberResult = await memberService.GetByIdAsync(memberId, ct);
        if (memberResult.IsFailure)
            return Result.Failure<bool>(memberResult.Error!);

        var otp = _otps.Values
            .Where(o => o.MemberId == memberId && o.Code == code && o.IsValid())
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefault();

        if (otp is null)
        {
            logger.LogWarning("OTP validation failed for member={MemberId}", memberId);
            return Result.Success(false);
        }

        otp.MarkUsed();
        logger.LogInformation("OTP validated for member={MemberId}", memberId);
        return Result.Success(true);
    }

    private static string GenerateCode() =>
        Random.Shared.Next(100_000, 999_999).ToString();
}
