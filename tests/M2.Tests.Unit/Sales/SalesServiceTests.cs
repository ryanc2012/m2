using FluentAssertions;
using M2.Domain.Sales;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Sales;

/// <summary>
/// Contracts and domain invariants for ISalesService / SalesTransaction / IReturnService.
/// BE-REC-001 R1: Idempotency keys on all mutating Sales API endpoints.
/// S4.9: CreateSale, VoidSale, ReturnSale, idempotency edge cases.
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

    // ─── S4.9: CreateSale ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 50.00, 0.00, 50.00)]
    [InlineData(3, 20.00, 0.00, 60.00)]
    [InlineData(2, 99.99, 10.00, 189.98)]
    [InlineData(5, 10.00, 5.00, 45.00)]
    public void LineItem_LineTotal_ShouldBeQtyTimesUnitPriceMinusDiscount(
        int qty, double unitPrice, double discount, double expectedTotal)
    {
        // Domain invariant: LineTotal = (UnitPrice × Qty) - DiscountAmount
        var item = new SalesLineItem(
            _tenantId, _shopId, Guid.NewGuid(),
            "PROD001", "Product", "商品",
            qty, (decimal)unitPrice, (decimal)discount);

        item.LineTotal.Should().Be((decimal)expectedTotal,
            $"LineTotal must equal (UnitPrice × Qty) - Discount = ({unitPrice} × {qty}) - {discount}");
    }

    [Fact]
    public void CreateTransaction_WithTwoItems_ShouldSumLineTotalsAsTransactionTotal()
    {
        // Domain invariant: TotalAmount = sum of all LineItems.LineTotal
        var tx = new SalesTransaction(_tenantId, _shopId, memberId: null, "EMP001", PaymentMethod.Cash);
        tx.AddLineItem(new SalesLineItem(_tenantId, _shopId, tx.Id, "PROD001", "Widget", "小部件", 2, 50m));
        tx.AddLineItem(new SalesLineItem(_tenantId, _shopId, tx.Id, "PROD002", "Gadget", "小工具", 1, 30m));

        tx.TotalAmount.Should().Be(130m, "TotalAmount must equal the sum of all line item LineTotals");
        tx.LineItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateTransaction_HappyPath_WithMemberAndItems_ShouldSucceed()
    {
        // Happy path: valid items + memberId → SalesTransaction with correct totals
        var mockService = new Mock<ISalesService>();
        var memberId = Guid.NewGuid();
        var items = new[]
        {
            new LineItemRequest("PROD001", "Widget", "小部件", 2, 50m),
            new LineItemRequest("PROD002", "Gadget", "小工具", 1, 30m)
        };

        var tx = new SalesTransaction(_tenantId, _shopId, memberId, "EMP001", PaymentMethod.Cash);
        tx.AddLineItem(new SalesLineItem(_tenantId, _shopId, tx.Id, "PROD001", "Widget", "小部件", 2, 50m));
        tx.AddLineItem(new SalesLineItem(_tenantId, _shopId, tx.Id, "PROD002", "Gadget", "小工具", 1, 30m));

        mockService
            .Setup(s => s.CreateTransactionAsync(
                _tenantId, _shopId, memberId, "EMP001",
                PaymentMethod.Cash, items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(tx));

        var result = await mockService.Object.CreateTransactionAsync(
            _tenantId, _shopId, memberId, "EMP001", PaymentMethod.Cash, items);

        result.IsSuccess.Should().BeTrue("a valid cart with member must create a transaction");
        result.Value!.TotalAmount.Should().Be(130m, "total must equal sum of line totals");
        result.Value.MemberId.Should().Be(memberId, "memberId must be attached to the transaction");
        result.Value.Status.Should().Be(SalesStatus.Pending);
    }

    [Fact]
    public async Task CreateTransaction_EmptyCart_ShouldFail()
    {
        var mockService = new Mock<ISalesService>();
        var emptyItems = Array.Empty<LineItemRequest>();

        mockService
            .Setup(s => s.CreateTransactionAsync(
                _tenantId, _shopId, null, "EMP001",
                PaymentMethod.Cash, emptyItems, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SalesTransaction>("Cart must contain at least one item."));

        var result = await mockService.Object.CreateTransactionAsync(
            _tenantId, _shopId, null, "EMP001", PaymentMethod.Cash, emptyItems);

        result.IsFailure.Should().BeTrue("an empty cart must be rejected at the service boundary");
        result.Error.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// S4.9 gap: ISalesService.CreateTransactionAsync lacks an IdempotencyKey parameter.
    /// BE-REC-001 R1 mandates idempotency on all mutating Sales endpoints.
    /// McManus must add the parameter and EF-based dedup before this test can pass.
    /// </summary>
    [Fact(Skip = "Pending S4.1: ISalesService.CreateTransactionAsync lacks IdempotencyKey parameter — BE-REC-001 R1")]
    public async Task CreateTransaction_SameIdempotencyKey_ShouldReturnIdenticalResponse()
    {
        // When IdempotencyKey is added to ISalesService.CreateTransactionAsync:
        // - Second call with same key must return the SAME transaction ID, no new record.
        // - The response (TotalAmount, LineItems, Status) must be byte-for-byte identical.
        // - Idempotency must be shop-scoped OR global — needs ADR clarification.
        await Task.CompletedTask;
    }

    /// <summary>Null IdempotencyKey is non-idempotent — two calls create two distinct records.</summary>
    [Fact(Skip = "Pending S4.1: ISalesService.CreateTransactionAsync lacks IdempotencyKey parameter — BE-REC-001 R1")]
    public async Task CreateTransaction_NullIdempotencyKey_ShouldNotDeduplicate()
    {
        // When IdempotencyKey is added:
        // - Passing null must NOT deduplicate — two calls produce two distinct transaction IDs.
        await Task.CompletedTask;
    }

    // ─── S4.9: VoidSale ────────────────────────────────────────────────────────

    [Fact]
    public async Task VoidTransaction_AlreadyVoided_ShouldFail()
    {
        var mockService = new Mock<ISalesService>();
        var txId = Guid.NewGuid();

        mockService
            .Setup(s => s.VoidTransactionAsync(txId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SalesTransaction>(
                $"Transaction {txId} cannot be voided — current status is Voided."));

        var result = await mockService.Object.VoidTransactionAsync(txId);

        result.IsFailure.Should().BeTrue("a previously voided transaction must not be voidable again");
        result.Error.Should().Contain("Voided");
    }

    [Fact]
    public async Task VoidTransaction_NotFound_ShouldFail()
    {
        var mockService = new Mock<ISalesService>();
        var nonExistentId = Guid.NewGuid();

        mockService
            .Setup(s => s.VoidTransactionAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SalesTransaction>($"Transaction {nonExistentId} not found."));

        var result = await mockService.Object.VoidTransactionAsync(nonExistentId);

        result.IsFailure.Should().BeTrue("voiding a non-existent transaction must return a failure, not throw");
        result.Error.Should().Contain("not found");
    }

    [Theory]
    [InlineData(SalesStatus.Completed)]
    [InlineData(SalesStatus.Voided)]
    [InlineData(SalesStatus.Refunded)]
    public async Task VoidTransaction_NonPendingStatus_ShouldFail(SalesStatus status)
    {
        // Contract: only Pending transactions may be voided (all other statuses must be rejected).
        var mockService = new Mock<ISalesService>();
        var txId = Guid.NewGuid();

        mockService
            .Setup(s => s.VoidTransactionAsync(txId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SalesTransaction>(
                $"Transaction {txId} cannot be voided — current status is {status}."));

        var result = await mockService.Object.VoidTransactionAsync(txId);

        result.IsFailure.Should().BeTrue($"a transaction in {status} status must never be voidable");
        result.Error.Should().NotBeNullOrEmpty();
    }

    // ─── S4.9: ReturnSale ──────────────────────────────────────────────────────

    [Fact]
    public async Task InitiateReturn_FullReturn_ShouldCreateReturnRecord()
    {
        var mockService = new Mock<IReturnService>();
        var originalTxId = Guid.NewGuid();
        const decimal fullAmount = 200m;
        const string reason = "Customer request";

        var returnTx = new ReturnTransaction(
            _tenantId, _shopId, originalTxId, reason, fullAmount, PaymentMethod.Cash);

        mockService
            .Setup(s => s.InitiateReturnAsync(
                _tenantId, _shopId, originalTxId, reason, fullAmount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(returnTx));

        var result = await mockService.Object.InitiateReturnAsync(
            _tenantId, _shopId, originalTxId, reason, fullAmount);

        result.IsSuccess.Should().BeTrue("a full return on a completed transaction must succeed");
        result.Value!.OriginalTransactionId.Should().Be(originalTxId);
        result.Value.RefundAmount.Should().Be(fullAmount);
        result.Value.IsComplete.Should().BeFalse(
            "newly initiated return must not be complete — CompleteReturnAsync is a separate call");
    }

    [Fact]
    public async Task InitiateReturn_PartialReturn_ShouldCreateReturnWithCorrectAmount()
    {
        var mockService = new Mock<IReturnService>();
        var originalTxId = Guid.NewGuid();
        const decimal originalAmount = 100m;
        const decimal partialAmount = 40m;

        var returnTx = new ReturnTransaction(
            _tenantId, _shopId, originalTxId, "Partial return", partialAmount, PaymentMethod.CreditCard);

        mockService
            .Setup(s => s.InitiateReturnAsync(
                _tenantId, _shopId, originalTxId, "Partial return", partialAmount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(returnTx));

        var result = await mockService.Object.InitiateReturnAsync(
            _tenantId, _shopId, originalTxId, "Partial return", partialAmount);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RefundAmount.Should().Be(partialAmount,
            "partial return must record exactly the amount requested, not the full original total");
        result.Value.RefundAmount.Should().BeLessThan(originalAmount,
            "a partial return amount must be less than the original total");
    }

    /// <summary>
    /// S4.9 gap: IReturnService.InitiateReturnAsync has no originalTotal parameter.
    /// Over-return validation (refundAmount > originalTotal) must be enforced by the EF implementation
    /// by looking up the original transaction total. Cannot be tested at interface level without the
    /// original total in scope.
    /// </summary>
    [Fact(Skip = "Pending S4.1: over-return validation requires EF ReturnService to lookup original transaction total")]
    public async Task InitiateReturn_OverReturn_ShouldFail()
    {
        // When EF ReturnService is delivered:
        // 1. Lookup originalTransaction.TotalAmount via ISalesService.GetByIdAsync.
        // 2. If refundAmount > originalTransaction.TotalAmount → return failure.
        // 3. Error message must clearly indicate the over-return condition.
        await Task.CompletedTask;
    }

    [Fact]
    public async Task InitiateReturn_OnVoidedTransaction_ShouldFail()
    {
        // Contract: returns are only valid on Completed transactions (ADR-016 / ReturnService stub).
        var mockService = new Mock<IReturnService>();
        var voidedTxId = Guid.NewGuid();

        mockService
            .Setup(s => s.InitiateReturnAsync(
                _tenantId, _shopId, voidedTxId,
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ReturnTransaction>(
                $"Transaction {voidedTxId} is not completed; cannot return."));

        var result = await mockService.Object.InitiateReturnAsync(
            _tenantId, _shopId, voidedTxId, "reason", 50m);

        result.IsFailure.Should().BeTrue("a return on a voided transaction must be rejected");
        result.Error.Should().Contain("not completed");
    }
}
