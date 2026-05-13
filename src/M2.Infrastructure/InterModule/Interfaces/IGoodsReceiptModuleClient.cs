using M2.Domain.GoodsReceipt.Dtos;
using M2.SharedKernel;

namespace M2.Infrastructure.InterModule.Interfaces;

public interface IGoodsReceiptModuleClient
{
    Task<Result<GoodsReceiptNoteDto>> CreateAsync(CreateGoodsReceiptPayload payload, CancellationToken ct = default);
    Task<Result<GoodsReceiptNoteDto>> ConfirmAsync(Guid id, CancellationToken ct = default);
    Task<Result<GoodsReceiptNoteDto>> RecordDiscrepancyAsync(Guid id, RecordDiscrepancyPayload payload, CancellationToken ct = default);
    Task<Result<GoodsReceiptNoteDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GoodsReceiptNoteDto>>> ListByShopAsync(Guid tenantId, Guid shopId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<Result> PostToSapAsync(Guid id, CancellationToken ct = default);
}
