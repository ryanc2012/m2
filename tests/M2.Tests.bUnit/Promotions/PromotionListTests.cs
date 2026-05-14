using Bunit;
using Bunit.TestDoubles;
using M2Portal.Pages.Promotions;
using M2Portal.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;

namespace M2.Tests.bUnit.Promotions;

public class PromotionListTests : IDisposable
{
    private readonly TestContext _ctx;

    public PromotionListTests()
    {
        _ctx = new TestContext();
        _ctx.Services.AddMudServices();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.AddTestAuthorization().SetAuthorized("TestUser");
    }

    [Fact]
    public void PromotionList_WhenNoPromotions_ShowsEmptyState()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        handler.When("/api/v1/promotions").Respond("application/json", "[]");
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:5002");
        _ctx.Services.AddScoped(_ => new PromotionService(httpClient));

        // Act
        var cut = _ctx.RenderComponent<PromotionList>();

        // Assert
        cut.WaitForAssertion(() =>
            Assert.Contains("No promotions yet", cut.Markup));
    }

    [Fact]
    public void PromotionList_WhenPromotionsLoaded_ShowsPromotionName()
    {
        // Arrange — use typed objects so enum values serialize as integers
        var promotions = new List<PromotionSummary>
        {
            new(
                Id: Guid.NewGuid(),
                NameEn: "Summer Sale",
                NameZht: "夏季特賣",
                Type: PromotionType.Percentage,
                Status: PromotionStatus.Active,
                StartDate: new DateOnly(2026, 6, 1),
                EndDate: new DateOnly(2026, 6, 30),
                IsStackable: false)
        };

        var handler = new MockHttpMessageHandler();
        handler.When("/api/v1/promotions")
               .Respond("application/json", JsonSerializer.Serialize(promotions));
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:5002");
        _ctx.Services.AddScoped(_ => new PromotionService(httpClient));

        // Act
        var cut = _ctx.RenderComponent<PromotionList>();

        // Assert — both EN and ZHT names appear in the table
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Summer Sale", cut.Markup);
            Assert.Contains("夏季特賣", cut.Markup);
        });
    }

    [Fact]
    public void PromotionList_WhenApiError_ShowsFailedToLoadAlert()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        handler.When("/api/v1/promotions")
               .Respond(HttpStatusCode.InternalServerError);
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:5002");
        _ctx.Services.AddScoped(_ => new PromotionService(httpClient));

        // Act
        var cut = _ctx.RenderComponent<PromotionList>();

        // Assert — error message rendered in MudAlert
        cut.WaitForAssertion(() =>
            Assert.Contains("Failed to load", cut.Markup));
    }

    public void Dispose() => _ctx.Dispose();
}
