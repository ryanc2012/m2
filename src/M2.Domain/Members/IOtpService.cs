using M2.SharedKernel;

namespace M2.Domain.Members;

public interface IOtpService
{
    Task<Result<OtpRequest>> GenerateAsync(Guid memberId, CancellationToken ct = default);
    Task<Result<bool>> ValidateAsync(Guid memberId, string code, CancellationToken ct = default);
}
