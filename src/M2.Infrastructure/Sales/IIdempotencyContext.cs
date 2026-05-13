namespace M2.Infrastructure.Sales;

/// <summary>
/// Scoped ambient context that carries an optional idempotency key from the endpoint handler
/// into <see cref="SalesService.CreateTransactionAsync"/> without polluting the domain interface.
/// The endpoint handler sets <see cref="Key"/> before invoking the service. (BE-REC-001 R1)
/// </summary>
public interface IIdempotencyContext
{
    string? Key { get; set; }
}

internal sealed class IdempotencyContext : IIdempotencyContext
{
    public string? Key { get; set; }
}
