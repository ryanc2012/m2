using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using M2.Domain.Approvals;
using M2.Domain.Approvals.Dtos;
using M2.SharedKernel;
using M2.Tests.Integration.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace M2.Tests.Integration.Approvals;

/// <summary>
/// End-to-end integration tests for the Approval workflow.
/// Each test spins up a dedicated <see cref="ApprovalWorkflowWebApplicationFactory"/> so that
/// the EF in-memory DB and Moq policy mock are fully isolated per test.
///
/// HTTP surface exercised: /modules/approvals/requests (POST, GET)
///                          /modules/approvals/requests/{id}/approve (POST)
///                          /modules/approvals/requests/{id}/reject  (POST)
/// EscalateAsync is tested via DI (no HTTP endpoint exists for escalation yet).
///
/// Trait: Category=Integration
/// </summary>
[Trait("Category", "Integration")]
public class ApprovalWorkflowIntegrationTests
{
    private readonly Guid _tenantId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _shopId   = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    // ---------------------------------------------------------------------------
    // Happy path — single-step approval
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task HappyPath_SingleStep_FinalStatusIsApproved()
    {
        // Arrange — policy MaxLevels=1 so the first approval completes the request
        await using var factory = new ApprovalWorkflowWebApplicationFactory();
        factory.SetDefaultPolicy(maxLevels: 1);
        using var client = factory.CreatePlatformClient();

        var entityId = Guid.NewGuid();

        // Act — create request
        var createPayload = new CreateApprovalPayload(_tenantId, _shopId, "Discount", entityId);
        var createResponse = await client.PostAsJsonAsync("/modules/approvals/requests", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await createResponse.Content.ReadFromJsonAsync<ApprovalRequestDto>();
        created.Should().NotBeNull();
        created!.Status.Should().Be("Pending");

        // Act — approve the single step
        var approvePayload = new ApproveRejectPayload(TestAuthHandler.TestUserId, "LGTM");
        var approveResponse = await client.PostAsJsonAsync(
            $"/modules/approvals/requests/{created.Id}/approve", approvePayload);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — final state is Approved
        var getResponse = await client.GetAsync($"/modules/approvals/requests/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var final = await getResponse.Content.ReadFromJsonAsync<ApprovalRequestDto>();
        final!.Status.Should().Be("Approved",
            "after approving the only configured step the request must be Approved");
    }

    // ---------------------------------------------------------------------------
    // Rejection path — reject at step 1
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task RejectionPath_RejectAtStep1_FinalStatusIsRejected()
    {
        // Arrange — policy MaxLevels=2 (default); rejection ignores level count
        await using var factory = new ApprovalWorkflowWebApplicationFactory();
        factory.SetDefaultPolicy(maxLevels: 2);
        using var client = factory.CreatePlatformClient();

        var entityId = Guid.NewGuid();

        // Create
        var createResponse = await client.PostAsJsonAsync(
            "/modules/approvals/requests",
            new CreateApprovalPayload(_tenantId, _shopId, "StockAdjust", entityId));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<ApprovalRequestDto>();

        // Reject
        var rejectPayload = new ApproveRejectPayload(TestAuthHandler.TestUserId, "Insufficient budget");
        var rejectResponse = await client.PostAsJsonAsync(
            $"/modules/approvals/requests/{created!.Id}/reject", rejectPayload);
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        var getResponse = await client.GetAsync($"/modules/approvals/requests/{created.Id}");
        var final = await getResponse.Content.ReadFromJsonAsync<ApprovalRequestDto>();
        final!.Status.Should().Be("Rejected",
            "rejecting at any step must immediately close the request as Rejected");
    }

    // ---------------------------------------------------------------------------
    // Escalation path — escalate pending request
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task EscalationPath_EscalatePendingRequest_StatusIsEscalatedWithReason()
    {
        // Escalation is not yet exposed via HTTP (endpoint gap noted in S4.10 decision).
        // Test via DI scope instead.
        await using var factory = new ApprovalWorkflowWebApplicationFactory();
        factory.SetDefaultPolicy(maxLevels: 2);

        // Seed a request via the real service
        await using var scope = factory.Services.CreateAsyncScope();
        var svc = scope.ServiceProvider.GetRequiredService<IApprovalService>();

        var entityId = Guid.NewGuid();
        var createResult = await svc.CreateRequestAsync(_tenantId, _shopId, "PurchaseOrder", entityId);
        createResult.IsSuccess.Should().BeTrue();

        var requestId = createResult.Value!.Id;
        const string reason = "Approver unavailable — escalating to regional manager";

        // Act — escalate
        var escalateResult = await svc.EscalateAsync(
            requestId, TestAuthHandler.TestUserId, reason);

        // Assert
        escalateResult.IsSuccess.Should().BeTrue();
        escalateResult.Value!.Status.Should().Be(ApprovalStatus.Escalated,
            "escalation must transition the request to Escalated");

        // Re-fetch to confirm persistence
        var fetchResult = await svc.GetRequestAsync(requestId);
        fetchResult.IsSuccess.Should().BeTrue();
        fetchResult.Value!.Status.Should().Be(ApprovalStatus.Escalated);
        fetchResult.Value.Steps.Should().ContainSingle(
            s => s.Comment != null && s.Comment.Contains("[ESCALATED]") && s.Comment.Contains(reason),
            "the escalation reason must be recorded as a step comment");
    }

    // ---------------------------------------------------------------------------
    // Multi-step path — two-step approval chain
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task MultiStepPath_TwoStepPolicy_StillPendingAfterStep1_ApprovedAfterStep2()
    {
        // Arrange — policy MaxLevels=2 (need two distinct approvals)
        await using var factory = new ApprovalWorkflowWebApplicationFactory();
        factory.SetDefaultPolicy(maxLevels: 2);
        using var client = factory.CreatePlatformClient();

        var entityId = Guid.NewGuid();

        // Create
        var createResponse = await client.PostAsJsonAsync(
            "/modules/approvals/requests",
            new CreateApprovalPayload(_tenantId, _shopId, "BudgetOverride", entityId));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<ApprovalRequestDto>();

        var approvePayload = new ApproveRejectPayload(TestAuthHandler.TestUserId, null);

        // Step 1 approval
        var approve1Response = await client.PostAsJsonAsync(
            $"/modules/approvals/requests/{created!.Id}/approve", approvePayload);
        approve1Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterStep1 = await approve1Response.Content.ReadFromJsonAsync<ApprovalRequestDto>();
        afterStep1!.Status.Should().Be("Pending",
            "request must remain Pending after step 1 when MaxLevels=2");
        afterStep1.CurrentStep.Should().Be(2,
            "step counter must advance after step 1 approval");

        // Step 2 approval
        var approve2Response = await client.PostAsJsonAsync(
            $"/modules/approvals/requests/{created.Id}/approve", approvePayload);
        approve2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterStep2 = await approve2Response.Content.ReadFromJsonAsync<ApprovalRequestDto>();
        afterStep2!.Status.Should().Be("Approved",
            "request must be fully Approved after all configured steps are approved");
    }
}
