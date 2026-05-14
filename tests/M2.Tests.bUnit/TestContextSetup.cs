using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using M2Portal.Services;

namespace M2.Tests.bUnit;

/// <summary>Extension helpers for setting up bUnit test context with MudBlazor services.</summary>
public static class TestContextSetup
{
    public static TestContext CreateWithMudBlazor()
    {
        var ctx = new TestContext();
        ctx.Services.AddMudServices();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }
}

/// <summary>No-op implementation of INotificationHubService for use in tests.</summary>
internal sealed class FakeNotificationHubService : INotificationHubService
{
    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void OnApprovalStateChanged(Func<Guid, string, Task> handler) { }
    public void OnNotificationReceived(Func<NotificationEntry, Task> handler) { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
