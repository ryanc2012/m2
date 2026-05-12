# Coding Standards — POS Enterprise Platform

> **Author:** Keyser (Lead / Architect)
> **Date:** 2026-05-12
> **Status:** Approved — Mandatory for all contributors

---

## Table of Contents

1. [C#/.NET Standards](#1-cnet-standards)
2. [Dart/Flutter Standards](#2-dartflutter-standards)
3. [Blazor Standards](#3-blazor-standards)
4. [REST API Design Standards](#4-rest-api-design-standards)
5. [Database Standards](#5-database-standards)
6. [Git Workflow](#6-git-workflow)
7. [Security Standards](#7-security-standards)
8. [Testing Standards](#8-testing-standards)

---

## 1. C#/.NET Standards

### 1.1 Naming Conventions

| Construct | Convention | Example |
|-----------|-----------|---------|
| Class | PascalCase | `SalesTransaction`, `PromotionService` |
| Interface | `I` prefix + PascalCase | `IPromotionService`, `ISapInventoryPort` |
| Abstract class | PascalCase (no special prefix) | `BaseAuditableEntity` |
| Record | PascalCase | `CreatePromotionCommand`, `DiscountResult` |
| Method | PascalCase | `CalculateDiscount()`, `GetActivePomotionsAsync()` |
| Async method | PascalCase + `Async` suffix | `CreateSaleAsync()`, `FindByIdAsync()` |
| Property | PascalCase | `TotalAmount`, `CreatedAt` |
| Private field | `_camelCase` | `_promotionRepository`, `_logger` |
| Local variable | camelCase | `totalDiscount`, `lineItem` |
| Parameter | camelCase | `promotionId`, `cancellationToken` |
| Constant | PascalCase (not ALL_CAPS) | `MaxRetryAttempts`, `DefaultPageSize` |
| Enum | PascalCase type + PascalCase members | `ApprovalStatus.Pending` |
| Generic type parameter | `T` prefix | `TEntity`, `TResult` |
| Extension method class | `{Type}Extensions` | `StringExtensions`, `QueryableExtensions` |

**Rules:**
- Do not use Hungarian notation (`strName`, `intCount` — forbidden)
- Do not abbreviate unless the abbreviation is a domain term (e.g., `POS`, `ECR`, `SAP`)
- Acronyms of 2 letters: both uppercase (`ID`, `IO`). Acronyms of 3+: PascalCase (`Bff`, `Api`, not `BFF`, `API` in identifiers)
- Boolean properties / variables: use `Is`, `Has`, `Can`, `Should` prefix (`IsActive`, `HasDiscount`)

### 1.2 File and Folder Structure

**Solution Layout:**

```
pos.sln
│
src/
├── Platform.Core/                  ← Domain models, interfaces, shared abstractions
│   ├── Domain/
│   │   ├── Sales/
│   │   ├── Promotions/
│   │   └── Members/
│   ├── Application/
│   │   ├── Sales/Commands/
│   │   ├── Sales/Queries/
│   │   └── Promotions/Commands/
│   └── Common/
│       ├── Interfaces/
│       └── Exceptions/
│
├── Platform.Infrastructure/        ← EF Core, SAP Adapter, external services
│   ├── Persistence/
│   │   ├── Configurations/
│   │   └── Migrations/
│   ├── SapAdapter/
│   └── ExternalServices/
│
├── Platform.Authorization/         ← Auth objects module
├── Platform.Approval/              ← Approval workflow module
├── Platform.Notification/          ← Notification module
│
├── MekaPosBff/                     ← Meka POS BFF
│   ├── Endpoints/
│   ├── Contracts/
│   │   ├── Requests/
│   │   └── Responses/
│   └── Program.cs
│
├── MekaPromosBff/                  ← Meka Promotions BFF
└── M2PortalBff/                    ← M2 Portal BFF
    └── Components/                 ← Blazor components (hosted in this project)

tests/
├── Platform.Core.Tests/
├── Platform.Infrastructure.Tests/
├── MekaPosBff.IntegrationTests/
└── Platform.Architecture.Tests/    ← ArchUnit-style boundary enforcement tests
```

**Rules:**
- One class per file; file name matches class name exactly
- Namespace must match folder structure exactly
- BFF projects contain no business logic — requests, responses, and endpoint wiring only
- Tests mirror the `src/` structure

### 1.3 Async/Await Patterns

```csharp
// ✅ CORRECT — always pass CancellationToken through
public async Task<SalesTransaction> CreateSaleAsync(
    CreateSaleCommand command,
    CancellationToken cancellationToken = default)
{
    var discount = await _discountCalculator.CalculateAsync(command, cancellationToken);
    // ...
    await _repository.AddAsync(transaction, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    return transaction;
}

// ✅ ConfigureAwait(false) in library/infrastructure code (not in ASP.NET Core handlers)
public async Task<SapPriceResult> GetPriceAsync(string materialCode, CancellationToken ct)
{
    var response = await _httpClient
        .GetFromJsonAsync<SapPriceResponse>(url, ct)
        .ConfigureAwait(false);
    return MapToResult(response);
}

// ❌ FORBIDDEN — blocking async code
var result = GetDataAsync().Result;        // deadlock risk
var result = GetDataAsync().GetAwaiter().GetResult(); // deadlock risk
Task.Run(() => GetDataAsync()).Wait();    // deadlock risk

// ❌ FORBIDDEN — fire-and-forget without exception handling
_ = ProcessAsync();   // exceptions are silently swallowed
```

**Rules:**
- Every public async method accepts `CancellationToken cancellationToken = default`
- `ConfigureAwait(false)` used in all `Platform.*` library projects (not in BFF/Blazor)
- Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` unless in a synchronous context with clear documentation
- Use `ValueTask<T>` only when profiling shows it's a hot path (default to `Task<T>`)

### 1.4 Exception Handling

**Custom Exception Hierarchy:**

```csharp
// Base
public abstract class PlatformException : Exception
{
    public string ErrorCode { get; }
    protected PlatformException(string errorCode, string message) : base(message)
        => ErrorCode = errorCode;
}

// Domain exceptions (business rule violations)
public class BusinessRuleViolationException : PlatformException { }
public class EntityNotFoundException : PlatformException { }
public class ConflictException : PlatformException { }

// Authorization
public class UnauthorizedException : PlatformException { }
public class ForbiddenException : PlatformException { }

// SAP integration exceptions
public class SapIntegrationException : PlatformException { }
public class SapFunctionalException : SapIntegrationException { }  // BAPI error messages
```

**Rules:**

```csharp
// ✅ Throw specific exceptions with context
throw new EntityNotFoundException("PROMOTION_NOT_FOUND",
    $"Promotion {promotionId} does not exist or is not active.");

// ✅ Catch only what you can handle; let the rest bubble
try
{
    await _sapClient.PostGoodsReceiptAsync(receipt, ct);
}
catch (SapFunctionalException ex) when (ex.ErrorCode == "MATERIAL_LOCKED")
{
    // Handle material lock specifically
    throw new ConflictException("GOODS_RECEIPT_CONFLICT",
        "Material is locked in SAP. Try again in a few minutes.", ex);
}
// Do NOT catch SapIntegrationException here — let it bubble to the global handler

// ✅ Global exception handler in BFF (maps to HTTP status)
app.UseExceptionHandler(builder => builder.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var response = ExceptionMapper.MapToErrorResponse(exception);
    context.Response.StatusCode = response.StatusCode;
    await context.Response.WriteAsJsonAsync(response.Body);
}));

// ❌ FORBIDDEN — swallowing exceptions
catch (Exception) { }

// ❌ FORBIDDEN — catching Exception broadly without re-throw
catch (Exception ex)
{
    _logger.LogError(ex, "Something failed");
    // missing re-throw — caller doesn't know about the failure
}
```

- Do NOT use exceptions for flow control (checking if a record exists before acting)
- Log exceptions at the point they cross a process boundary, not at every layer
- Include the original exception as `innerException` when wrapping

### 1.5 Dependency Injection Patterns

```csharp
// ✅ Constructor injection — only injectable dependencies (not primitives)
public sealed class PromotionService(
    IPromotionRepository repository,
    IDiscountCalculator discountCalculator,
    ILogger<PromotionService> logger)
{
    // primary constructor — no field declarations needed
}

// ✅ Register by interface, scoped per request for domain services
services.AddScoped<IPromotionService, PromotionService>();
services.AddScoped<IPromotionRepository, PromotionRepository>();
services.AddSingleton<IDiscountCalculator, DiscountCalculator>(); // stateless — singleton ok

// ✅ Module registration via extension methods
public static IServiceCollection AddPromotionModule(this IServiceCollection services)
{
    services.AddScoped<IPromotionService, PromotionService>();
    services.AddScoped<IPromotionRepository, PromotionRepository>();
    services.AddDbContext<PromotionDbContext>(/* ... */);
    return services;
}

// ❌ FORBIDDEN — service locator pattern
var service = serviceProvider.GetService<IPromotionService>(); // don't do this in app code

// ❌ FORBIDDEN — more than 4 constructor dependencies (signals God class)
// Refactor: introduce a Facade, or decompose the class
```

### 1.6 Logging Standards

**Framework:** Serilog with structured logging. Every log entry must be parseable by a log aggregator.

```csharp
// ✅ Structured logging with semantic properties
_logger.LogInformation(
    "Sale created. {TransactionId} {TotalAmount} {CustomerId}",
    transaction.Id, transaction.TotalAmount, transaction.CustomerId);

// ✅ Log levels
// Verbose/Trace  — detailed diagnostic (dev only, never in production)
// Debug          — diagnostic information useful during development
// Information    — significant business events (sale created, member registered)
// Warning        — recoverable issues, unexpected but handled (SAP retry #2)
// Error          — unhandled exceptions crossing a boundary
// Critical/Fatal — system-wide failure requiring immediate attention

// ✅ Always include exception as first argument for Error/Critical
_logger.LogError(ex, "SAP goods receipt failed. {ReceiptId} {AttemptNumber}",
    receiptId, attemptNumber);

// ❌ FORBIDDEN — string interpolation in log messages (loses structured properties)
_logger.LogInformation($"Sale {transactionId} created for {amount}");

// ❌ FORBIDDEN — sensitive data in logs
_logger.LogInformation("Customer phone {PhoneNumber}", customer.Phone); // PII — forbidden
```

**Serilog Configuration (minimum):**
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "MekaPosBff")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.ApplicationInsights(telemetryClient, TelemetryConverter.Traces)
    .CreateLogger();
```

### 1.7 Entity / DTO Separation

```csharp
// ✅ Entity — lives in Domain, has identity, no external exposure
public class SalesTransaction : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public decimal TotalAmount { get; private set; }
    private readonly List<SalesLineItem> _lineItems = [];
    public IReadOnlyCollection<SalesLineItem> LineItems => _lineItems.AsReadOnly();

    // Domain behavior on the entity
    public void AddLineItem(Product product, int quantity)
    {
        // validation + business rule
        _lineItems.Add(new SalesLineItem(product, quantity));
        RecalculateTotal();
    }
}

// ✅ Request DTO — BFF boundary, validated by FluentValidation
public record CreateSaleRequest(
    string CashierId,
    string RegisterId,
    IReadOnlyList<SaleLineItemRequest> LineItems);

// ✅ Response DTO — BFF boundary, shaped for the client
public record SaleResponse(
    Guid TransactionId,
    decimal TotalAmount,
    decimal DiscountAmount,
    string ReceiptNumber);

// ✅ Command/Query — Application layer CQRS (MediatR)
public record CreateSaleCommand(
    Guid CashierId,
    Guid RegisterId,
    IReadOnlyList<SaleLineItem> LineItems) : IRequest<SalesTransaction>;
```

**Rules:**
- Entities are never serialized to the API response directly
- DTOs have no behavior (no methods)
- Use Mapperly for entity ↔ DTO mapping — no manual mapping loops
- Request DTOs live in BFF projects; Commands/Queries live in Application layer

### 1.8 Repository Pattern Standards

```csharp
// ✅ Generic repository interface in Platform.Core
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(TEntity entity, CancellationToken ct = default);
}

// ✅ Specific repository for domain queries
public interface IPromotionRepository : IRepository<Promotion>
{
    Task<IReadOnlyList<Promotion>> GetActivePromotionsAsync(
        DateOnly date, CancellationToken ct = default);
    Task<Promotion?> FindByCodeAsync(string code, CancellationToken ct = default);
}

// ✅ Unit of Work for multi-aggregate transactions
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

**Rules:**
- Repositories return domain entities or `null` (never throw `NotFoundException`)
- Callers throw `EntityNotFoundException` when a `null` result is unacceptable
- Repositories do not contain business logic
- Do not expose `IQueryable<T>` beyond the repository boundary
- The `IUnitOfWork` is committed in Application layer handlers, never in repositories

---

## 2. Dart/Flutter Standards

### 2.1 State Management: Riverpod

**Decision: Flutter Riverpod (with code generation)**

Rationale over alternatives:

| Framework | Verdict |
|-----------|---------|
| **Riverpod** ✅ | Compile-time safe, testable without BuildContext, excellent async state support, scales from simple to complex. The modern Flutter standard. |
| Bloc | More boilerplate for simple cases; better for event-driven flows with complex sequences. Justifiable for the POS transaction flow but over-engineered for promotion browsing. |
| Provider | Riverpod's predecessor. No type safety, context-dependent. Do not use. |
| GetX | Combines routing + state + DI in one package. Anti-pattern — mixes concerns, hard to test. Forbidden. |

```dart
// ✅ Provider declaration (with code generation)
@riverpod
Future<List<Promotion>> activePromotions(ActivePromotionsRef ref) async {
  final client = ref.watch(promotionApiClientProvider);
  return client.getActivePromotions();
}

// ✅ Consuming in a widget
class PromotionListScreen extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final promotionsAsync = ref.watch(activePromotionsProvider);
    return promotionsAsync.when(
      data: (promotions) => PromotionList(promotions: promotions),
      loading: () => const LoadingIndicator(),
      error: (error, _) => ErrorDisplay(message: error.toString()),
    );
  }
}
```

### 2.2 Widget Decomposition Rules

- A widget that exceeds **100 lines** must be decomposed
- A widget that has more than **3 levels of nested builders** must be decomposed
- Extract repeated UI patterns into named widgets in `lib/shared/widgets/`
- Stateless over Stateful — use `ConsumerWidget` (Riverpod) before reaching for `StatefulWidget`
- Avoid `BuildContext` across async gaps — capture before await

### 2.3 Naming Conventions and Folder Structure

**Feature-first layout:**
```
lib/
├── app/                        ← App-level: router, theme, DI
│   ├── router.dart
│   └── theme.dart
│
├── features/
│   ├── promotions/
│   │   ├── data/
│   │   │   ├── promotion_api_client.dart
│   │   │   └── promotion_repository.dart
│   │   ├── domain/
│   │   │   └── promotion.dart
│   │   ├── presentation/
│   │   │   ├── promotion_list_screen.dart
│   │   │   ├── promotion_detail_screen.dart
│   │   │   └── widgets/
│   │   │       └── promotion_card.dart
│   │   └── providers/
│   │       └── promotion_providers.dart
│   │
│   ├── sales/
│   └── member/
│
├── shared/
│   ├── widgets/
│   ├── extensions/
│   └── utils/
│
└── main.dart
```

**Naming rules:**

| Construct | Convention | Example |
|-----------|-----------|---------|
| File | `snake_case.dart` | `promotion_list_screen.dart` |
| Class / Widget | PascalCase | `PromotionListScreen` |
| Variable / function | camelCase | `activePromotions`, `calculateDiscount()` |
| Constant | `lowerCamelCase` (not ALL_CAPS unless truly global) | `defaultPageSize` |
| Private member | `_camelCase` | `_promotionRepository` |
| Provider | `camelCase` + `Provider` suffix | `activePromotionsProvider` |

### 2.4 Error Handling — Result Types

Use `Result<T, E>` pattern (via `fpdart` or equivalent) for operations that can fail in expected ways:

```dart
// ✅ Use sealed class Result for domain operations
sealed class Result<T> {
  const Result();
}
class Success<T> extends Result<T> {
  final T value;
  const Success(this.value);
}
class Failure<T> extends Result<T> {
  final AppError error;
  const Failure(this.error);
}

// ✅ Repository method signature
Future<Result<Promotion>> getPromotionByCode(String code);

// ✅ Handling at call site
final result = await repository.getPromotionByCode(code);
switch (result) {
  case Success(:final value): showPromotion(value);
  case Failure(:final error): showError(error.message);
}

// ❌ FORBIDDEN — swallowing errors
try {
  await someOperation();
} catch (_) {}
```

### 2.5 API Client Standards

**Framework:** Dio with custom interceptors.

```dart
// ✅ Typed API client via Retrofit/Dio generator
@RestApi()
abstract class PromotionApiClient {
  factory PromotionApiClient(Dio dio) = _PromotionApiClient;

  @GET('/api/v1/promotions')
  Future<List<PromotionDto>> getActivePromotions();

  @GET('/api/v1/promotions/{id}')
  Future<PromotionDto> getPromotion(@Path() String id);
}

// ✅ Interceptor chain: auth → retry → logging
Dio createDio(String baseUrl, ApiKeyStore keyStore) {
  return Dio(BaseOptions(baseUrl: baseUrl))
    ..interceptors.addAll([
      ApiKeyInterceptor(keyStore),    // injects X-Api-Key header
      RetryInterceptor(retries: 3),   // retries on 5xx / network error
      LoggingInterceptor(),           // dev-only verbose logging
    ]);
}
```

- Base URLs are environment-configured — never hardcoded
- All responses are typed DTOs — never `Map<String, dynamic>` in domain code
- API errors map to `AppError` domain types at the client layer

### 2.6 Localization

- Use `flutter_localizations` + `intl` package + ARB files
- All user-facing strings in `lib/l10n/*.arb` — no hardcoded strings in widget code
- Locale: Thai (th_TH) primary, English (en) fallback
- Date/currency formatting via `intl.DateFormat` and `intl.NumberFormat` — no manual formatting

---

## 3. Blazor Standards

### 3.1 Component Naming and File Organization

```
M2PortalBff/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   ├── Pages/
│   │   ├── Promotions/
│   │   │   ├── PromotionList.razor
│   │   │   ├── PromotionCreate.razor
│   │   │   └── PromotionEdit.razor
│   │   └── Approvals/
│   │       └── ApprovalQueue.razor
│   └── Shared/
│       ├── LoadingSpinner.razor
│       ├── ErrorAlert.razor
│       └── ConfirmDialog.razor
```

**Rules:**
- Component file: PascalCase, `.razor` extension, one component per file
- Page components have `@page "/route"` directive and live in `Pages/`
- Reusable sub-components live in `Shared/` or a feature subfolder
- No logic-heavy components exceed 150 lines — extract to code-behind

### 3.2 Code-Behind (Chosen Approach)

**Decision: Code-behind (`.razor.cs`) for all components with logic.**

Rationale: Inline C# in Razor (`@code { }`) mixes markup and logic. Code-behind produces two clean files — the Razor template and a pure C# partial class — which are independently testable, readable, and reviewable.

```razor
{{!-- PromotionCreate.razor --}}
@page "/promotions/create"
@inject IPromotionApiClient PromotionApi
@inject NavigationManager Nav

<PageTitle>Create Promotion</PageTitle>
<PromotionForm Model="@_model" OnValidSubmit="@HandleSubmit" />
```

```csharp
// PromotionCreate.razor.cs
public partial class PromotionCreate
{
    private CreatePromotionRequest _model = new();

    private async Task HandleSubmit()
    {
        var result = await PromotionApi.CreatePromotionAsync(_model);
        Nav.NavigateTo($"/promotions/{result.Id}");
    }
}
```

**Rules:**
- `@code { }` blocks: permitted only for trivial property declarations (1–3 lines)
- Any method or business logic goes in `.razor.cs` partial class
- No `async void` event handlers — use `async Task`

### 3.3 Service Injection Patterns

```csharp
// ✅ Constructor injection in code-behind (preferred — testable)
public partial class PromotionList
{
    [Inject] private IPromotionApiClient PromotionApi { get; set; } = default!;
    [Inject] private ILogger<PromotionList> Logger { get; set; } = default!;
}

// ✅ Register in DI container
builder.Services.AddHttpClient<IPromotionApiClient, PromotionApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BffBaseUrl"]!);
});
```

- Services injected via `[Inject]` or constructor, never via `@inject` in `.razor` for testability
- `HttpClient` always registered via `IHttpClientFactory` — never `new HttpClient()`

### 3.4 Form Validation Standards

```razor
<EditForm Model="@_model" OnValidSubmit="@HandleSubmit">
    <FluentValidationValidator />
    <ValidationSummary />

    <MudTextField @bind-Value="_model.Name"
                  Label="Promotion Name"
                  For="@(() => _model.Name)" />

    <MudButton ButtonType="ButtonType.Submit">Create</MudButton>
</EditForm>
```

```csharp
// ✅ FluentValidation validator for request models
public class CreatePromotionRequestValidator : AbstractValidator<CreatePromotionRequest>
{
    public CreatePromotionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.ValidFrom).LessThan(x => x.ValidTo);
    }
}
```

**Rules:**
- Always use `FluentValidation` — never `DataAnnotations` (`[Required]`, `[MaxLength]`) for domain models
- `DataAnnotations` are acceptable for simple Blazor form models only
- Every form has a `<ValidationSummary />` — do not rely solely on inline field errors
- Disable submit button while processing (`IsSubmitting` flag pattern)

---

## 4. REST API Design Standards

### 4.1 URL Conventions

```
# ✅ Plural nouns, kebab-case, no verbs in URL
GET    /api/v1/promotions
GET    /api/v1/promotions/{id}
POST   /api/v1/promotions
PUT    /api/v1/promotions/{id}
PATCH  /api/v1/promotions/{id}
DELETE /api/v1/promotions/{id}

# ✅ Nested resources for owned sub-resources
GET    /api/v1/sales-transactions/{id}/line-items
POST   /api/v1/sales-transactions/{id}/void

# ✅ Actions on resources (when REST verbs don't fit)
POST   /api/v1/promotions/{id}/activate
POST   /api/v1/approval-workflows/{id}/approve
POST   /api/v1/approval-workflows/{id}/reject

# ❌ FORBIDDEN — verbs in URLs
GET    /api/v1/getPromotions
POST   /api/v1/createPromotion
POST   /api/v1/promotions/doActivate

# ❌ FORBIDDEN — camelCase or PascalCase segments
GET    /api/v1/salesTransactions
GET    /api/v1/SalesTransactions
```

### 4.2 Standard Response Envelope

All API responses use a consistent envelope structure.

**Success — single item:**
```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Buy 2 Get 1 Free",
    "discountPercent": 33.3,
    "validFrom": "2026-05-01",
    "validTo": "2026-05-31",
    "status": "Active"
  },
  "meta": null
}
```

**Success — collection with pagination:**
```json
{
  "data": [
    { "id": "...", "name": "..." },
    { "id": "...", "name": "..." }
  ],
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 143,
    "totalPages": 8
  }
}
```

**Error response:**
```json
{
  "error": {
    "code": "PROMOTION_NOT_FOUND",
    "message": "Promotion with ID 3fa85f64 does not exist.",
    "details": [],
    "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
  }
}
```

**Validation error (422 Unprocessable Entity):**
```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "One or more validation errors occurred.",
    "details": [
      { "field": "discountPercent", "message": "Must be between 0 and 100." },
      { "field": "validFrom", "message": "ValidFrom must be before ValidTo." }
    ],
    "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
  }
}
```

### 4.3 HTTP Status Codes

| Scenario | Code |
|----------|------|
| Successful GET / PUT / PATCH | 200 OK |
| Successful POST (created) | 201 Created + `Location` header |
| Successful DELETE | 204 No Content |
| Validation failure | 422 Unprocessable Entity |
| Not found | 404 Not Found |
| Auth token invalid / missing | 401 Unauthorized |
| Insufficient permissions | 403 Forbidden |
| Duplicate / conflict | 409 Conflict |
| Server error | 500 Internal Server Error |
| SAP / downstream unavailable | 503 Service Unavailable |

### 4.4 Versioning Strategy

URL path versioning (`/api/v1/`, `/api/v2/`). See §5 of ARCHITECTURE.md for full rationale.

- `v1` is the current version until a breaking change is required
- Breaking change definition: removing a field, renaming a field, changing a type, removing an endpoint
- Additive changes (new optional fields, new endpoints) do NOT require a version bump
- Deprecated versions return `Deprecation: true` and `Sunset: {date}` response headers

### 4.5 Pagination Standard

Offset-based pagination for all list endpoints:

```
GET /api/v1/promotions?page=1&pageSize=20
```

- Default page size: 20
- Maximum page size: 100 (reject requests for more)
- Response includes `meta.totalCount` and `meta.totalPages`
- `page` is 1-indexed

Cursor-based pagination may be used for high-volume feeds (e.g., transaction history) where offset pagination causes performance issues — document when adopting.

---

## 5. Database Standards

### 5.1 Table and Column Naming

```sql
-- ✅ Tables: PascalCase, plural (matches EF Core convention)
SalesTransactions
PromotionFormulas
ApprovalWorkflows
MemberProfiles

-- ✅ Columns: PascalCase
TransactionId      -- not transaction_id, not transactionId
TotalAmount
CreatedAt
CreatedByUserId

-- ✅ Junction tables: Entity1Entity2 (alphabetical order)
PromotionProducts   -- Promotion ↔ Product
UserRoles           -- User ↔ Role

-- ❌ FORBIDDEN — snake_case, ALL_CAPS, prefixes
sales_transactions
tbl_promotions
TRANSACTION_ID
```

### 5.2 Primary Key Strategy

```sql
-- ✅ GUID primary keys for all domain entities (application-generated)
Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID()

-- Sequential GUIDs (NEWSEQUENTIALID) preferred over random NEWID()
-- for index fragmentation control in SQL Server.
-- For PostgreSQL: use gen_random_uuid() or uuid7 extension.
```

**Rationale:** GUID PKs allow the application to assign the ID before the database insert, enabling optimistic offline patterns and avoiding round-trips for the inserted ID.

**Exception:** Reference/lookup tables (e.g., `PromotionTypes`, `TransactionStatuses`) may use `INT` identity PKs for storage efficiency.

### 5.3 Standard Audit Columns

Every mutable entity table **must** include:

```sql
CreatedAt       DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME()
CreatedBy       NVARCHAR(100)       NOT NULL   -- UserId or system identity
UpdatedAt       DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME()
UpdatedBy       NVARCHAR(100)       NOT NULL
```

Implemented via EF Core `SaveChangesInterceptor`:

```csharp
public class AuditInterceptor(ICurrentUserService currentUser) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = currentUser.UserId;
            }
            entry.Entity.UpdatedAt = DateTime.UtcNow;
            entry.Entity.UpdatedBy = currentUser.UserId;
        }
        return base.SavingChangesAsync(...);
    }
}
```

### 5.4 Soft Delete Standard

All domain entities support soft delete via `IsDeleted` flag:

```sql
IsDeleted   BIT             NOT NULL DEFAULT 0
DeletedAt   DATETIME2(7)    NULL
DeletedBy   NVARCHAR(100)   NULL
```

EF Core global query filter:
```csharp
modelBuilder.Entity<SalesTransaction>().HasQueryFilter(e => !e.IsDeleted);
```

**Rules:**
- Hard delete is forbidden for domain entities — use soft delete
- EF Core global query filter ensures soft-deleted records are never returned
- Audit reports may require bypassing the filter — use `IgnoreQueryFilters()` explicitly

### 5.5 Migration Tool and File Naming

**Tool:** EF Core Migrations

```bash
# ✅ Migration naming convention: PascalCase description of the change
dotnet ef migrations add AddPromotionFormula
dotnet ef migrations add AddMemberProfilePhoneIndex
dotnet ef migrations add SalesTransactionAddDiscountColumns
```

**Rules:**
- One logical change per migration — do not bundle unrelated changes
- Migrations are committed alongside the code change that requires them
- Never edit a migration after it has been applied to any environment
- Migration rollback scripts must be tested before applying forward migrations to production

### 5.6 Index Naming Conventions

```sql
-- ✅ Format: IX_{Table}_{Columns}
IX_SalesTransactions_CashierId
IX_SalesTransactions_CreatedAt
IX_PromotionFormulas_StatusValidFrom

-- ✅ Unique indexes: UX_{Table}_{Columns}
UX_Members_PhoneNumber
UX_ApiKeys_KeyHash

-- ✅ Foreign key constraints: FK_{DependentTable}_{PrincipalTable}_{Column}
FK_SalesLineItems_SalesTransactions_TransactionId
```

---

## 6. Git Workflow

### 6.1 Branching Strategy: GitHub Flow (simplified)

```
main (protected)
  ↑
  ├── feature/POS-123-add-promotion-discount
  ├── fix/POS-145-void-transaction-wrong-status
  ├── chore/POS-150-update-ef-core-9
  └── release/1.2.0   (tagged, deployed to prod)
```

**Rules:**
- `main` is always deployable — every commit on `main` must pass CI
- No direct commits to `main` — all changes via Pull Request
- Feature branches are short-lived (< 5 days) — if longer, use feature flags
- One branch per ticket — no "catch-all" branches

### 6.2 Branch Naming Conventions

```
# Format: {type}/{ticket-id}-{brief-description}

feature/POS-123-promotion-discount-calculation
fix/POS-145-void-transaction-status-bug
chore/POS-150-upgrade-dotnet-9
docs/POS-160-update-architecture-diagram
refactor/POS-170-extract-sap-adapter
release/1.2.0
```

**Types:**
- `feature/` — new functionality
- `fix/` — bug fixes
- `chore/` — maintenance, upgrades, tooling
- `docs/` — documentation only
- `refactor/` — code restructuring without behavior change
- `release/` — release preparation branches

### 6.3 Commit Message Format — Conventional Commits

```
{type}({scope}): {description}

[optional body]

[optional footer]
```

**Examples:**
```
feat(promotions): add percentage discount calculation for multi-buy rules

fix(pos-bff): resolve null reference in void transaction endpoint

chore(deps): upgrade EF Core to 9.0.3

refactor(sap-adapter): extract retry policy to Polly configuration

docs(architecture): update BFF diagram with SignalR hub

BREAKING CHANGE: rename /api/v1/promos to /api/v1/promotions
```

**Types:**

| Type | When to use |
|------|------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test` | Adding or correcting tests |
| `chore` | Maintenance, dependency updates, tooling |
| `perf` | Performance improvement |
| `ci` | CI/CD pipeline changes |

**Scopes:** Use module/project name: `promotions`, `pos-bff`, `sap-adapter`, `approval`, `notification`, `auth`

### 6.4 Pull Request Process

**Required for merge:**
1. CI passes (build + tests + linting)
2. Minimum **1 approving review** from a team member (architecture-impacting changes: **2 reviews**)
3. No unresolved review comments
4. Branch is up-to-date with `main`

**Merge strategy:** Squash and merge — keeps `main` history clean (one commit per feature/fix)

**PR description must include:**
- What: what does this PR do?
- Why: why is this change needed (link ticket)?
- How to test: steps for the reviewer

**Auto-delete source branch** on merge is enabled.

---

## 7. Security Standards

### 7.1 No Hardcoded Secrets

```csharp
// ❌ FORBIDDEN — any of these in source code
var connectionString = "Server=prod-db;Password=SuperSecret123!";
var apiKey = "sk-live-abc123...";
var sapPassword = "SapP@ssw0rd";

// ✅ CORRECT — always from configuration
var connectionString = configuration.GetConnectionString("AppDb");
// Backed by Key Vault in production, user-secrets in dev
```

**Git pre-commit hook:** `detect-secrets` or `gitleaks` must be configured — any secret pattern in a commit blocks the push. This is enforced via CI as well.

### 7.2 Environment Variable Naming

```
# Naming: SCREAMING_SNAKE_CASE, hierarchical with double underscore
ConnectionStrings__AppDb=...
Entra__TenantId=...
Entra__ClientId=...
Sap__BaseUrl=...
Sap__Username=...
Fcm__ServiceAccountKeyPath=...
ApiManagement__SubscriptionKey=...
```

### 7.3 Input Validation Rules

```csharp
// ✅ Validate at BFF boundary with FluentValidation
public class RegisterMemberRequestValidator : AbstractValidator<RegisterMemberRequest>
{
    public RegisterMemberRequestValidator()
    {
        // Phone: Thai mobile format
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^0[6-9]\d{8}$")
            .WithMessage("Must be a valid Thai mobile number");

        // Text fields: max length to prevent DB overflow and injection surface
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[\w\s\-\.]+$");
    }
}
```

**Rules:**
- Validate all inputs at the BFF boundary — assume nothing from the client is safe
- Use allowlist regex for structured fields (phone, ID, codes) — not blocklist
- Reject unexpected content types and oversized payloads (configure `MaxRequestBodySize`)
- SQL injection: not possible with EF Core parameterized queries — but still forbidden to use raw SQL with string interpolation

### 7.4 OWASP Top 10 Per Technology

**ASP.NET Core BFF:**
- Enable HTTPS redirection and HSTS
- Configure CORS with explicit allowed origins — no wildcard (`*`) in production
- Use `Content-Security-Policy` headers via middleware
- Rate limiting via APIM and ASP.NET Core RateLimiter middleware

**Blazor (M2 Portal):**
- Blazor Server: Use SignalR with authentication — unauthenticated circuit connections forbidden
- Avoid `@((MarkupString)userContent)` — XSS risk. Never render user-supplied HTML
- CSRF protection: Blazor forms include anti-forgery tokens by default — do not disable

**Flutter:**
- Store API keys in secure storage (`flutter_secure_storage`) — never in `SharedPreferences`
- Certificate pinning for production API calls
- Obfuscate release builds (`--obfuscate --split-debug-info`)

**Database:**
- No raw SQL with user input — use EF Core or parameterized commands
- Database user has minimum required privileges (no `sysadmin` for the app user)
- Connection strings never in logs

---

## 8. Testing Standards

> Full test strategy is owned by Verbal (Tester). This section defines minimum standards for all contributors.

### 8.1 Test File Naming and Structure

```
tests/
├── Platform.Core.Tests/
│   ├── Domain/
│   │   ├── Sales/
│   │   │   └── SalesTransactionTests.cs     ← mirrors src/ structure
│   │   └── Promotions/
│   │       └── DiscountCalculatorTests.cs
│   └── Application/
│       └── Sales/
│           └── CreateSaleCommandHandlerTests.cs
│
├── Platform.Infrastructure.Tests/
│   └── SapAdapter/
│       └── SapODataClientTests.cs
│
└── MekaPosBff.IntegrationTests/
    └── Endpoints/
        └── SalesEndpointTests.cs
```

### 8.2 Test Naming Convention

```csharp
// Format: MethodUnderTest_Scenario_ExpectedBehavior
[Fact]
public async Task CalculateDiscount_WhenBuy2Get1ActivePromotion_ReturnsCorrectDiscountAmount()

[Fact]
public async Task CreateSale_WhenCashierNotAuthorized_ThrowsForbiddenException()

[Theory]
[InlineData(0)]
[InlineData(-1)]
public async Task AddLineItem_WhenQuantityLessThanOne_ThrowsBusinessRuleViolationException(int qty)
```

### 8.3 Minimum Coverage Expectations

| Layer | Minimum Coverage |
|-------|-----------------|
| Domain (entities, value objects, calculators) | 90% |
| Application (command/query handlers) | 80% |
| Infrastructure (adapters, repositories) | 60% (integration tests preferred over unit) |
| BFF endpoints | Integration tests for all happy paths + key error paths |
| Flutter features | Widget tests for all screens, unit tests for providers |

- Coverage is a floor, not a ceiling — write tests because they prevent regressions, not to hit a number
- Tests that mock everything and verify nothing real are worse than no tests — test behavior, not implementation
- CI blocks merges if coverage drops below floor for Domain and Application layers
