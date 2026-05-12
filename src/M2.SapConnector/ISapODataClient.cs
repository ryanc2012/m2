using M2.SharedKernel;

namespace M2.SapConnector;

/// <summary>SAP OData REST client interface (ADR-006 primary channel).</summary>
public interface ISapODataClient
{
    Task<Result<IReadOnlyList<SapProduct>>> GetProductsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Result<SapOrganisation>> GetOrganisationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SapEmployee>>> GetEmployeesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

// Lightweight DTO stubs — domain mapping happens in Infrastructure anti-corruption layer
public record SapProduct(string MaterialNumber, BilingualText Description, string UnitOfMeasure);
public record SapOrganisation(string CompanyCode, BilingualText Name);
public record SapEmployee(string PersonnelNumber, string FirstName, string LastName, string Position);
