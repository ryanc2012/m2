using M2.Domain.Members.Dtos;
using M2.SharedKernel;

namespace M2.Infrastructure.InterModule.Interfaces;

public interface IMembersModuleClient
{
    Task<Result<MemberDto>> RegisterAsync(RegisterMemberPayload payload, CancellationToken ct = default);
    Task<Result<MemberDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<MemberDto>> GetByQrCodeAsync(string qrCode, CancellationToken ct = default);
    Task<Result<MemberDto>> UpdateProfileAsync(Guid id, UpdateMemberProfilePayload payload, CancellationToken ct = default);
    Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default);
}
