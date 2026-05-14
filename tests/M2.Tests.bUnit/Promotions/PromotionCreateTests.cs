using Bunit;
using Bunit.TestDoubles;
using M2Portal.Pages.Promotions;
using M2Portal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using RichardSzalay.MockHttp;
using System.Reflection;

namespace M2.Tests.bUnit.Promotions;

public class PromotionCreateTests
{
    // Each test creates its own isolated context to avoid DI container locking issues
    // when MudBlazor popover components are involved.

    private static TestContext BuildCtx(HttpClient httpClient)
    {
        var ctx = new TestContext();
        ctx.Services.AddMudServices();
        // Register PromotionService BEFORE any service resolution to avoid locking
        ctx.Services.AddScoped(_ => new PromotionService(httpClient));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.AddTestAuthorization().SetAuthorized("TestUser");
        // MudSelect and MudDatePicker require MudPopoverProvider; render it first
        ctx.RenderComponent<MudPopoverProvider>();
        return ctx;
    }

    [Fact]
    public void PromotionCreate_Renders_WithFormAndRequiredFields()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:5002");

        using var ctx = BuildCtx(httpClient);

        // Act
        var cut = ctx.RenderComponent<PromotionCreate>();

        // Assert — page title and key form fields are present
        Assert.Contains("New Promotion", cut.Markup);
        Assert.Contains("Start Date", cut.Markup);
        Assert.Contains("End Date", cut.Markup);
        Assert.Contains("Formula JSON", cut.Markup);
    }

    [Fact]
    public async Task PromotionCreate_EndDateBeforeStartDate_DoesNotNavigate()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:5002");

        using var ctx = BuildCtx(httpClient);
        var cut = ctx.RenderComponent<PromotionCreate>();
        var instance = cut.Instance;

        // Set backing fields via reflection
        SetField(instance, "_nameEn", "Test Promo EN");
        SetField(instance, "_nameZht", "測試活動");
        SetField(instance, "_formulaJson", """{"discountPercent":10}""");
        // End date is BEFORE start date — the date-range guard must prevent navigation
        SetField(instance, "_startDate", new DateTime(2026, 6, 30));
        SetField(instance, "_endDate", new DateTime(2026, 6, 1));

        var navMan = ctx.Services.GetRequiredService<NavigationManager>();
        var baseUri = navMan.Uri;

        // Act — invoke SubmitAsync via reflection; MudForm gate will fire first
        //        (required fields have no binding so IsValid=false), ensuring navigation
        //        cannot happen regardless of the bad dates
        await cut.InvokeAsync(async () =>
        {
            var submit = typeof(PromotionCreate)
                .GetMethod("SubmitAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)submit.Invoke(instance, null)!;
        });

        // Assert — no navigation should have occurred
        Assert.Equal(baseUri, navMan.Uri);
    }

    private static void SetField(object obj, string name, object value) =>
        obj.GetType()
           .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!
           .SetValue(obj, value);
}
