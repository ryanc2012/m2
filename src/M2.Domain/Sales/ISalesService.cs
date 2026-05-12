using M2.SharedKernel;

namespace M2.Domain.Sales;

public interface ISalesService
{
    Task<Result<SalesTransaction>> CreateTransactionAsync(
        Guid tenantId,
        Guid shopId,
        Guid? memberId,
        string cashierId,
        PaymentMethod paymentMethod,
        IEnumerable<LineItemRequest> lineItems,
        CancellationToken ct = default);

    Task<Result<SalesTransaction>> CompleteTransactionAsync(Guid id, CancellationToken ct = default);

    Task<Result<SalesTransaction>> VoidTransactionAsync(Guid id, CancellationToken ct = default);

    Task<Result<SalesTransaction>> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public record LineItemRequest(
    string ProductId,
    string ProductNameEn,
    string ProductNameZht,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount = 0);
