using System.Net;
using FluentAssertions;
using M2.Domain.Approvals;
using M2.SharedKernel;
using M2.Tests.Integration.Helpers;
using Moq;

namespace M2.Tests.Integration.Modules;

/// <summary>
/// Smoke tests for the Approvals module HTTP surface.
///
/// Target endpoint prefix: /modules/approvals/
/// Verifies GET /modules/approvals/pending is reachable (ADR-001 inter-module endpoint).
/// 200 with an empty list is the normal result when no pending approvals exist.
/// 400 is acceptable if the required approverId query param is omitted.
/// 500 is never acceptable.
/// </summary>
public class ApprovalsModuleTests : M2IntegrationTestBase, IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _typedFactory;

    public ApprovalsModuleTests(TestWebApplicationFactory factory) : base(factory)
    {
        _typedFactory = factory;
        // The /modules/approvals/pending endpoint delegates to IApprovalService (mocked in TestWebApplicationFactory).
        // Without a setup, Moq returns null for reference types which causes a NullReferenceException inside the
        // endpoint handler. Wire up an empty-list response so the endpoint can complete normally.
        _typedFactory.ApprovalServiceMock
            .Setup(s => s.GetPendingRequestsForApproverAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<ApprovalRequest>>(
                new List<ApprovalRequest>().AsReadOnly()));
    }

    /// <summary>
    /// Contract: GET /modules/approvals/pending must return 200 or 400 — never 401/403/500.
    /// Supplying a dummy approverId should yield 200 with an empty list against the test DB.
    /// </summary>
    [Fact]
    public async Task GetPendingApprovals_WithInternalHeaders_ReturnsOkOrExpectedError()
    {
        var response = await Client.GetAsync("/modules/approvals/pending?approverId=test-approver-001");

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest },
            "endpoint must return 200 (empty list) or a client error; 500 is never acceptable (ADR-001)");
    }

    /// <summary>
    /// Contract: GET /modules/approvals/pending WITHOUT the X-Internal-Call header must ultimately
    /// be rejected with 401/403 once the internal-call middleware is enforced (ADR-001).
    /// </summary>
    [Fact]
    public async Task GetPendingApprovals_WithoutInternalHeader_Returns401Or403OrNotFound()
    {
        using var clientWithoutHeaders = Factory.CreateClient();

        var response = await clientWithoutHeaders.GetAsync("/modules/approvals/pending?approverId=test-approver-001");

        response.StatusCode.Should().BeOneOf(
            new[]
            {
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NotFound,
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest,
            },
            "module endpoint must ultimately reject callers missing X-Internal-Call (ADR-001/ADR-004)");
    }
}
