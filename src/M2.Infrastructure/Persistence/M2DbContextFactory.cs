using M2.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace M2.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core CLI tools (dotnet ef migrations add, etc.).
/// Reads the connection string from environment variable M2_DB or falls back to a local dev default.
/// </summary>
public class M2DbContextFactory : IDesignTimeDbContextFactory<M2DbContext>
{
    public M2DbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("M2_DB")
            ?? "Host=localhost;Port=5432;Database=m2_dev;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<M2DbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(M2DbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "m2");
            })
            .Options;

        return new M2DbContext(options);
    }
}
