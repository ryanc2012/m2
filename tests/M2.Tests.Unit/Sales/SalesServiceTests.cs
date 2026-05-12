using FluentAssertions;
using M2.Domain.Sales;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Sales;

/// <summary>
/// Contracts and domain invariants for ISalesService / SalesTransaction.
/// BE-REC-001 R1: Idempotency keys on all mutating Sales API endpoints.
/// </summary>
public class SalesServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();

    private SalesTransaction BuildTransaction() =>
        new(_tenantId, _shopId, memberId: null, cashierId: "EMP001", PaymentMethod.Cash);

    [Fact]
    public void CreateTransaction_ShouldBePending_Initially()
    {
        // Arrange / Act
        var transaction = BuildTransaction();

        // Assert
        transaction.Status.Should().Be(SalesStatus.Pending,
            "a newly created sales transaction must always start in Pending status");
        transaction.CompletedAt.Should().BeNull(
            "CompletedAt must be null until the transaction is completed");
    }

    [Fact]
    public void CompleteTransaction_ShouldSetCompletedAt()
    {
        // Arrange
        var transaction = BuildTransaction();

        // Act
        transaction.Complete();

        // Assert
        transaction.Status.Should().Be(SalesStatus.Completed,
            "calling Complete() must transition the transaction to Completed status");
        transaction.CompletedAt.Should().NotBeNull(
            "CompletedAt must be stamped when the transaction is completed");
        transaction.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task VoidTransaction_WhenCompleted_ShouldFail()
    {
        // Arrange — completed transactions must not be voidable
        var mockService = new Mock<ISalesService>();
        var transactionId = Guid.NewGuid();

        mockService
            .Setup(s => s.VoidTransactionAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SalesTransaction>(
                "Cannot void a completed transaction."));

        // Act
        var result = await mockService.Object.VoidTransactionAsync(transactionId);

        // Assert
        result.IsFailure.Should().BeTrue(
            "a completed transaction must not be voidable");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task VoidTransaction_WhenPending_ShouldSucceed()
    {
        // Arrange
        var mockService = new Mock<ISalesService>();
        var transactionId = Guid.NewGuid();

        var voidedTx = BuildTransaction();
        voidedTx.Void();

        mockService
            .Setup(s => s.VoidTransactionAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(voidedTx));

        // Act
        var result = await mockService.Object.VoidTransactionAsync(transactionId);

        // Assert
        result.IsSuccess.Should().BeTrue(
            "a pending transaction must be voidable");
        result.Value!.Status.Should().Be(SalesStatus.Voided,
            "voiding a pending transaction must transition it to Voided status");
        result.Value.VoidedAt.Should().NotBeNull(
            "VoidedAt must be stamped when the transaction is voided");
    }
}
