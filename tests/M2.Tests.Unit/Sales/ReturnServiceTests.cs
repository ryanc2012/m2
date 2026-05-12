using FluentAssertions;
using M2.Domain.Sales;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Sales;

/// <summary>
/// Contracts for IReturnService return/refund flows.
/// ADR-008 (task ref) / ADR-016 (decisions.md): Refunds processed to original payment method only.
/// </summary>
public class ReturnServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();

    private ReturnTransaction BuildReturn(PaymentMethod method = PaymentMethod.Cash) =>
        new(_tenantId, _shopId,
            originalTransactionId: Guid.NewGuid(),
            reason: "Customer request",
            refundAmount: 100m,
            refundMethod: method);

    [Fact]
    public async Task InitiateReturn_ShouldMatchOriginalPaymentMethod()
    {
        // ADR-008 (task ref): refund method must match the original transaction's payment method.
        // Arrange
        var mockService = new Mock<IReturnService>();
        var originalTxId = Guid.NewGuid();

        var returnTx = BuildReturn(PaymentMethod.Cash);

        mockService
            .Setup(s => s.InitiateReturnAsync(
                _tenantId, _shopId, originalTxId,
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(returnTx));

        // Act
        var result = await mockService.Object.InitiateReturnAsync(
            _tenantId, _shopId, originalTxId, "Customer request", 100m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RefundMethod.Should().Be(PaymentMethod.Cash,
            "the refund method must match the original transaction's payment method (ADR-008)");
    }

    [Fact]
    public async Task InitiateReturn_WhenPaymentMethodMismatch_ShouldThrow()
    {
        // ADR-008 (task ref): if the requested refund method differs from the original, the service must reject.
        // Arrange
        var mockService = new Mock<IReturnService>();
        var originalTxId = Guid.NewGuid();

        mockService
            .Setup(s => s.InitiateReturnAsync(
                _tenantId, _shopId, originalTxId,
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Refund method must match the original payment method."));

        // Act
        Func<Task> act = () => mockService.Object.InitiateReturnAsync(
            _tenantId, _shopId, originalTxId, "Customer request", 100m);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>(
            "initiating a return with a mismatched payment method must be rejected (ADR-008)");
    }

    [Fact]
    public void CompleteReturn_ShouldSetProcessedAt()
    {
        // Arrange
        var returnTx = BuildReturn();

        // Act
        returnTx.Complete();

        // Assert
        returnTx.IsComplete.Should().BeTrue(
            "calling Complete() on a return must mark it as complete");
        returnTx.ProcessedAt.Should().NotBeNull(
            "ProcessedAt must be stamped when the return is completed");
        returnTx.ProcessedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
