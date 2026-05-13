using M2.Domain.Sales.Dtos;
using M2.SharedKernel;

namespace M2.Infrastructure.InterModule.Interfaces;

public interface ISalesModuleClient
{
    Task<Result<SalesTransactionDto>> CreateTransactionAsync(CreateTransactionPayload payload, CancellationToken ct = default);
    Task<Result<SalesTransactionDto>> CompleteTransactionAsync(Guid id, CancellationToken ct = default);
    Task<Result<SalesTransactionDto>> VoidTransactionAsync(Guid id, CancellationToken ct = default);
    Task<Result<SalesTransactionDto>> GetTransactionByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ReturnTransactionDto>> InitiateReturnAsync(InitiateReturnPayload payload, CancellationToken ct = default);
    Task<Result<ReturnTransactionDto>> CompleteReturnAsync(Guid id, CancellationToken ct = default);
}
