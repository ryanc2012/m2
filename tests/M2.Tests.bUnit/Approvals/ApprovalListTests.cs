using Bunit;
using Bunit.TestDoubles;
using M2Portal.Pages.Approvals;
using M2Portal.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;

namespace M2.Tests.bUnit.Approvals;

public class ApprovalListTests : IDisposable
{
    private readonly TestContext _ctx;

    public ApprovalListTests()
    {
        _ctx = new TestContext();
        _ctx.Services.AddMudServices();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.AddTestAuthorization().SetAuthorized("TestUser");
        _ctx.Services.AddScoped<INotificationHubService>(_ => new FakeNotificationHubService());
    }

    [Fact]
    public void ApprovalList_WhenNoPendingApprovals_ShowsAllCaughtUpMessage()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        handler.When("/api/v1/approvals*").Respond("application/json", "[]");
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:5002");
        _ctx.Services.AddScoped(_ => new ApprovalService(httpClient));

        // Act
        var cut = _ctx.RenderComponent<ApprovalList>();

        // Assert
        cut.WaitForAssertion(() =>
            Assert.Contains("All caught up", cut.Markup));
    }

    [Fact]
    public void ApprovalList_WhenApprovalsLoaded_ShowsApprovalRows()
    {
        // Arrange — use typed objects so ApprovalStatus enum serializes as an integer
        var approvals = new List<ApprovalRequest>
        {
            new(
                Id: Guid.NewGuid(),
                EntityType: "Promotion",
                EntityId: "promo-001",
                RequestedBy: "alice@example.com",
                RequestedAt: DateTimeOffset.UtcNow,
                Status: ApprovalStatus.Pending)
        };

        var handler = new MockHttpMessageHandler();
        handler.When("/api/v1/approvals*")
               .Respond("application/json", JsonSerializer.Serialize(approvals));
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:5002");
        _ctx.Services.AddScoped(_ => new ApprovalService(httpClient));

        // Act
        var cut = _ctx.RenderComponent<ApprovalList>();

        // Assert — entity type and requestor appear in the rendered table
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Promotion", cut.Markup);
            Assert.Contains("alice@example.com", cut.Markup);
        });
    }

    [Fact]
    public void ApprovalList_QuickApprove_CallsApprovalEndpoint()
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var approvals = new List<ApprovalRequest>
        {
            new(
                Id: approvalId,
                EntityType: "Promotion",
                EntityId: "promo-002",
                RequestedBy: "bob@example.com",
                RequestedAt: DateTimeOffset.UtcNow,
                Status: ApprovalStatus.Pending)
        };

        var approveCallMade = false;
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "/api/v1/approvals*")
               .Respond("application/json", JsonSerializer.Serialize(approvals));
        handler.When(HttpMethod.Post, $"/api/v1/approvals/{approvalId}/approve")
               .Respond(_ =>
               {
                   approveCallMade = true;
                   return new HttpResponseMessage(HttpStatusCode.OK);
               });

        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:5002");
        _ctx.Services.AddScoped(_ => new ApprovalService(httpClient));

        // Act
        var cut = _ctx.RenderComponent<ApprovalList>();
        cut.WaitForAssertion(() => Assert.Contains("bob@example.com", cut.Markup));

        // Click the Approve button (use LINQ since CSS :contains is non-standard)
        cut.FindAll("button")
           .First(b => b.TextContent.Contains("Approve"))
           .Click();

        // Assert — the approve POST was called
        cut.WaitForAssertion(() => Assert.True(approveCallMade));
    }

    public void Dispose() => _ctx.Dispose();
}
