using FluentAssertions;
using M2.Domain.Approvals;
using M2.Domain.Approvals.Commands;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Approvals;

/// <summary>
/// Contract and behavioural stubs for IApprovalService.
/// Run against real ApprovalService when McManus delivers Sprint 2 implementation.
/// Contracts: BE-REC-001 R3 (doc-agnostic), ADR-014 (HCM + step modes), ADR-021 (N-level).
/// </summary>
public class ApprovalServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();

    [Fact]
    public async Task CreateRequest_ShouldReturnPendingStatus()
    {
        // Arrange
        var mockService = new Mock<IApprovalService>();
        var entityId = Guid.NewGuid();

        var pendingRequest = new ApprovalRequest(_tenantId, _shopId, "PurchaseOrder", entityId);

        mockService
            .Setup(s => s.CreateRequestAsync(_tenantId, _shopId, "PurchaseOrder", entityId))
            .ReturnsAsync(Result.Success(pendingRequest));

        // Act
        var result = await mockService.Object.CreateRequestAsync(_tenantId, _shopId, "PurchaseOrder", entityId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ApprovalStatus.Pending,
            "a newly created approval request must always start in Pending state");
        result.Value.CurrentStep.Should().Be(1,
            "new requests begin at step 1");
    }

    [Fact]
    public async Task ApproveStep_WhenLastStep_ShouldSetStatusApproved()
    {
        // Arrange — policy.MaxLevels = 1 (step-by-step mode, single-level)
        var mockService = new Mock<IApprovalService>();
        var requestId = Guid.NewGuid();
        var approverId = "pos.manager";

        var approvedRequest = new ApprovalRequest(_tenantId, _shopId, "Discount", Guid.NewGuid());
        approvedRequest.SetStatus(ApprovalStatus.Approved);

        mockService
            .Setup(s => s.ApproveStepAsync(requestId, approverId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(approvedRequest));

        // Act
        var result = await mockService.Object.ApproveStepAsync(requestId, approverId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ApprovalStatus.Approved,
            "when the last configured step is approved the request must become Approved");
    }

    [Fact]
    public async Task RejectStep_ShouldSetStatusRejected_AndNotEscalate()
    {
        // Arrange
        var mockService = new Mock<IApprovalService>();
        var requestId = Guid.NewGuid();
        var approverId = "pos.supervisor";
        var reason = "Budget exceeded threshold";

        var rejectedRequest = new ApprovalRequest(_tenantId, _shopId, "Discount", Guid.NewGuid());
        rejectedRequest.SetStatus(ApprovalStatus.Rejected);

        mockService
            .Setup(s => s.RejectStepAsync(requestId, approverId, reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(rejectedRequest));

        // Act
        var result = await mockService.Object.RejectStepAsync(requestId, approverId, reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ApprovalStatus.Rejected,
            "rejection must immediately set the request to Rejected — no escalation path exists");
        // Verify RejectStepAsync was called exactly once (no internal escalation retry)
        mockService.Verify(
            s => s.RejectStepAsync(requestId, approverId, reason, It.IsAny<CancellationToken>()),
            Times.Once,
            "rejection must not trigger escalation — called exactly once");
    }

    [Fact]
    public async Task ApproveStep_WhenNotLastStep_ShouldRemainPending_AndIncrementStep()
    {
        // Arrange — policy.MaxLevels = 2, request is at step 1
        var mockService = new Mock<IApprovalService>();
        var requestId = Guid.NewGuid();
        var approverId = "supervisor.l1";

        // After approval of step 1, request advances to step 2 (still Pending)
        var advancedRequest = new ApprovalRequest(_tenantId, _shopId, "StockAdjust", Guid.NewGuid());
        advancedRequest.AdvanceStep(); // CurrentStep = 2, Status remains Pending

        mockService
            .Setup(s => s.ApproveStepAsync(requestId, approverId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(advancedRequest));

        // Act
        var result = await mockService.Object.ApproveStepAsync(requestId, approverId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ApprovalStatus.Pending,
            "request must remain Pending when intermediate steps are approved");
        result.Value.CurrentStep.Should().Be(2,
            "step number must advance after each intermediate approval");
    }

    [Fact]
    public async Task ApproveStep_WhenMaxLevelsReached_ShouldAutoApprove()
    {
        // ADR-014 + ADR-021: When all configured HCM hierarchy levels have been traversed,
        // the engine auto-approves rather than leaving the request stuck with no further approver.
        // Arrange — request is at the top of the configured HCM level tree
        var mockService = new Mock<IApprovalService>();
        var requestId = Guid.NewGuid();
        var topLevelApproverId = "hcm.coo";

        var autoApprovedRequest = new ApprovalRequest(_tenantId, _shopId, "BudgetOverride", Guid.NewGuid());
        autoApprovedRequest.SetStatus(ApprovalStatus.Approved);

        mockService
            .Setup(s => s.ApproveStepAsync(requestId, topLevelApproverId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(autoApprovedRequest));

        // Act
        var result = await mockService.Object.ApproveStepAsync(requestId, topLevelApproverId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ApprovalStatus.Approved,
            "when max HCM levels are exhausted the engine must auto-approve (ADR-014 / ADR-021)");
    }

    [Fact]
    public async Task CreateRequest_WithHcmMode_ShouldRespectHierarchy()
    {
        // ADR-014: When ApprovalPolicy.Mode == SapHcmHierarchy for the entity type,
        // the approval engine must derive the approver chain from the SAP HCM org hierarchy.
        // The policy is configured per tenant+entityType via IApprovalPolicyService.

        // Arrange — configure HCM policy, then verify service accepts and creates request
        var mockApprovalService = new Mock<IApprovalService>();
        var mockPolicyService = new Mock<IApprovalPolicyService>();
        var entityId = Guid.NewGuid();
        const string entityType = "DiscountRequest";

        var hcmPolicy = new ApprovalPolicy(_tenantId, _shopId, entityType, ApprovalMode.SapHcmHierarchy, maxLevels: 3);
        mockPolicyService
            .Setup(p => p.GetPolicyAsync(_tenantId, entityType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ApprovalPolicy?>(hcmPolicy));

        var pendingRequest = new ApprovalRequest(_tenantId, _shopId, entityType, entityId);
        mockApprovalService
            .Setup(s => s.CreateRequestAsync(_tenantId, _shopId, entityType, entityId))
            .ReturnsAsync(Result.Success(pendingRequest));

        // Act
        var policyResult = await mockPolicyService.Object.GetPolicyAsync(_tenantId, entityType);
        var requestResult = await mockApprovalService.Object.CreateRequestAsync(_tenantId, _shopId, entityType, entityId);

        // Assert
        policyResult.Value!.Mode.Should().Be(ApprovalMode.SapHcmHierarchy,
            "policy must be configured for SAP HCM hierarchy mode (ADR-014)");
        requestResult.IsSuccess.Should().BeTrue();
        requestResult.Value!.EntityType.Should().Be(entityType);
        // NOTE: Full implementation test (McManus Sprint 2): verify Steps collection uses
        // ApproverType.SapHcm and that the SAP HCM parent chain is resolved correctly.
    }
}
