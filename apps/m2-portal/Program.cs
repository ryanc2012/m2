using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Entra ID authentication via Microsoft.Identity.Web (ADR-007)
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization(options =>
{
    // All routes require authentication by default
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();

// MudBlazor component library
builder.Services.AddMudServices();

// Approval service — calls M2PortalBff (base URL configured in appsettings)
builder.Services.AddHttpClient<M2Portal.Services.ApprovalService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["M2PortalBff:BaseUrl"] ?? "https://localhost:5001");
});

// Promotion service — calls M2PortalBff (same base URL)
builder.Services.AddHttpClient<M2Portal.Services.PromotionService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["M2PortalBff:BaseUrl"] ?? "https://localhost:5001");
});

// Goods Receipt service
builder.Services.AddHttpClient<M2Portal.Services.GoodsReceiptService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["M2PortalBff:BaseUrl"] ?? "https://localhost:5001");
});

// Dashboard service
builder.Services.AddHttpClient<M2Portal.Services.DashboardService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["M2PortalBff:BaseUrl"] ?? "https://localhost:5001");
});
builder.Services.AddScoped<M2Portal.Services.IDashboardService>(sp =>
    sp.GetRequiredService<M2Portal.Services.DashboardService>());

// Notification log service
builder.Services.AddHttpClient<M2Portal.Services.NotificationLogService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["M2PortalBff:BaseUrl"] ?? "https://localhost:5001");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

