namespace M2.SharedKernel;

public static class WellKnownTenants
{
    /// <summary>Single-tenant deployment default. Use everywhere a tenantId parameter is required.</summary>
    public static readonly Guid Default = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
