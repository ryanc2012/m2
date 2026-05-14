using FluentAssertions;
using M2.Domain.Authorization;
using M2.Infrastructure;
using M2.Infrastructure.Authorization;
using M2.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace M2.Tests.Unit.Authorization;

/// <summary>
/// Unit tests for AuthorizationService — permit/deny/field-constraint/cache semantics.
/// Uses EF Core InMemory for DB isolation and a real MemoryCache per test group.
/// S5.10 / ADR-004 / ADR-016.
/// </summary>
public class AuthorizationServiceTests : IDisposable
{
    private readonly M2DbContext _db;
    private readonly IMemoryCache _cache;
    private readonly AuthorizationService _sut;

    private static readonly Guid TenantId = WellKnownTenants.Default;
    private static readonly Guid ShopId = Guid.Empty;
    private const string UserId = "test-user-auth-001";
    private const string AuthObject = "M_PROMOTION_MANAGE";

    public AuthorizationServiceTests()
    {
        var inMemorySp = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<M2DbContext>()
            .UseInMemoryDatabase($"AuthzUnitTestDb-{Guid.NewGuid()}")
            .UseInternalServiceProvider(inMemorySp)
            .Options;

        _db = new M2DbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new AuthorizationService(_db, _cache);
    }

    public void Dispose()
    {
        _db.Dispose();
        _cache.Dispose();
    }

    // ── helpers ──────────────────────────────────────────────────────────────────

    private static ClaimsPrincipal MakePrincipal(string userId) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "Test"));

    private async Task<(AuthorizationRole role, RoleAuthorizationObject authObj)> SeedRoleWithObject(
        string authObject = AuthObject)
    {
        var role = new AuthorizationRole(TenantId, "TestRole", "Test role");
        _db.AuthorizationRoles.Add(role);
        await _db.SaveChangesAsync();

        var authObj = new RoleAuthorizationObject(TenantId, role.Id, authObject);
        _db.RoleAuthorizationObjects.Add(authObj);
        await _db.SaveChangesAsync();

        return (role, authObj);
    }

    private async Task SeedActiveAssignment(Guid roleId, string userId = UserId)
    {
        var assignment = new UserRoleAssignment(
            TenantId, ShopId, userId, roleId,
            validFrom: DateTimeOffset.UtcNow.AddDays(-1));
        _db.UserRoleAssignments.Add(assignment);
        await _db.SaveChangesAsync();
    }

    // ── Test 1 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_UnknownUser_ReturnsDeny()
    {
        // A principal with no NameIdentifier or sub claim must always be denied.
        var emptyPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _sut.CheckAsync(emptyPrincipal, AuthObject);

        result.Should().Be(AuthCheckResult.Deny,
            "a principal without NameIdentifier or sub claim has no identity to look up");
    }

    // ── Test 2 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_UserWithMatchingRole_ReturnsPermit()
    {
        var (role, _) = await SeedRoleWithObject();
        await SeedActiveAssignment(role.Id);

        var result = await _sut.CheckAsync(MakePrincipal(UserId), AuthObject);

        result.Should().Be(AuthCheckResult.Permit,
            "a user with an active role that grants the requested authObject must be permitted");
    }

    // ── Test 3 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_UserWithNoRoles_ReturnsDeny()
    {
        // No UserRoleAssignment seeded — the DB is empty for this user.
        var result = await _sut.CheckAsync(MakePrincipal(UserId), AuthObject);

        result.Should().Be(AuthCheckResult.Deny,
            "a user with zero role assignments must be denied");
    }

    // ── Test 4 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_UserWithRoleButDifferentAuthObject_ReturnsDeny()
    {
        var (role, _) = await SeedRoleWithObject("M_SALES_PROCESS");
        await SeedActiveAssignment(role.Id);

        var result = await _sut.CheckAsync(MakePrincipal(UserId), AuthObject /* M_PROMOTION_MANAGE */);

        result.Should().Be(AuthCheckResult.Deny,
            "a role that grants M_SALES_PROCESS must not permit M_PROMOTION_MANAGE");
    }

    // ── Test 5 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_UserWithExpiredRoleAssignment_ReturnsDeny()
    {
        var (role, _) = await SeedRoleWithObject();

        var expired = new UserRoleAssignment(
            TenantId, ShopId, UserId, role.Id,
            validFrom: DateTimeOffset.UtcNow.AddDays(-30),
            validTo: DateTimeOffset.UtcNow.AddDays(-1)); // expired yesterday
        _db.UserRoleAssignments.Add(expired);
        await _db.SaveChangesAsync();

        var result = await _sut.CheckAsync(MakePrincipal(UserId), AuthObject);

        result.Should().Be(AuthCheckResult.Deny,
            "an assignment whose ValidTo is in the past is no longer active");
    }

    // ── Test 6 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_UserWithFutureRoleAssignment_ReturnsDeny()
    {
        var (role, _) = await SeedRoleWithObject();

        var future = new UserRoleAssignment(
            TenantId, ShopId, UserId, role.Id,
            validFrom: DateTimeOffset.UtcNow.AddDays(1)); // starts tomorrow
        _db.UserRoleAssignments.Add(future);
        await _db.SaveChangesAsync();

        var result = await _sut.CheckAsync(MakePrincipal(UserId), AuthObject);

        result.Should().Be(AuthCheckResult.Deny,
            "an assignment whose ValidFrom is in the future must not yet be active");
    }

    // ── Test 7 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_WithFieldConstraint_Permit()
    {
        var (role, authObj) = await SeedRoleWithObject();
        await SeedActiveAssignment(role.Id);

        var fieldValue = new ObjectFieldValue(TenantId, authObj.Id, "ACTVT", "02");
        _db.ObjectFieldValues.Add(fieldValue);
        await _db.SaveChangesAsync();

        // Use an isolated cache so the previous tests' cache entries don't interfere.
        using var freshCache = new MemoryCache(new MemoryCacheOptions());
        var freshSut = new AuthorizationService(_db, freshCache);

        var result = await freshSut.CheckAsync(MakePrincipal(UserId), AuthObject, fields: ["ACTVT"]);

        result.Should().Be(AuthCheckResult.Permit,
            "when the requested field name (ACTVT) is present in FieldValues, the check must pass");
    }

    // ── Test 8 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_WithFieldConstraint_Deny()
    {
        var (role, authObj) = await SeedRoleWithObject();
        await SeedActiveAssignment(role.Id);

        // Only "ACTVT" is a permitted field; "BUKRS" is not listed.
        var fieldValue = new ObjectFieldValue(TenantId, authObj.Id, "ACTVT", "01");
        _db.ObjectFieldValues.Add(fieldValue);
        await _db.SaveChangesAsync();

        using var freshCache = new MemoryCache(new MemoryCacheOptions());
        var freshSut = new AuthorizationService(_db, freshCache);

        var result = await freshSut.CheckAsync(MakePrincipal(UserId), AuthObject, fields: ["BUKRS"]);

        result.Should().Be(AuthCheckResult.Deny,
            "requesting field BUKRS which is absent from FieldValues must be denied");
    }

    // ── Test 9 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_CacheTTL_SecondCallUsesCache()
    {
        // Arrange: seed an active role assignment — first call returns Permit.
        var (role, _) = await SeedRoleWithObject();
        await SeedActiveAssignment(role.Id);

        // Act 1 — DB is queried; result (Permit) is cached.
        var first = await _sut.CheckAsync(MakePrincipal(UserId), AuthObject);
        first.Should().Be(AuthCheckResult.Permit, "first call must query DB and return Permit");

        // Remove the assignment via EF change tracking (InMemory does not support ExecuteDeleteAsync).
        // If the service re-queried the DB, it would now return Deny.
        var assignments = await _db.UserRoleAssignments
            .Where(a => a.UserId == UserId)
            .ToListAsync();
        _db.UserRoleAssignments.RemoveRange(assignments);
        await _db.SaveChangesAsync();

        // Act 2 — cache hit; DB is NOT re-queried within the 5-min TTL window.
        var second = await _sut.CheckAsync(MakePrincipal(UserId), AuthObject);
        second.Should().Be(AuthCheckResult.Permit,
            "second call within the TTL window must return the cached Permit, not re-query and get Deny");
    }
}
