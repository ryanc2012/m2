using FluentAssertions;
using M2.Domain.Sales;

namespace M2.Tests.Unit.Sales;

/// <summary>
/// Structural tests confirming ECR integration is deferred post-MVP.
/// ADR-010 (task ref) / ADR-009 (decisions.md): No ECR concrete implementation.
/// </summary>
public class EcrServiceTests
{
    [Fact]
    public void IEcrService_ExistsAsInterface_NotImplemented()
    {
        // ADR-010 (task ref): ECR integration is deferred post-MVP.
        // Verify the interface exists as a placeholder but has no concrete implementation.

        // Arrange
        var domainAssembly = typeof(IEcrService).Assembly;
        var interfaceType = typeof(IEcrService);

        // Assert — interface is an interface (not a class)
        interfaceType.IsInterface.Should().BeTrue(
            "IEcrService must be an interface — ECR integration is deferred (ADR-010)");

        // Assert — no concrete class implements it in the domain assembly
        var concreteImplementations = domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t))
            .ToList();

        concreteImplementations.Should().BeEmpty(
            "no concrete ECR implementation should exist in the domain — ECR is out of scope for MVP (ADR-010)");
    }
}
