using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M2.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create the m2 schema. All module tables will be added in subsequent migrations.
        migrationBuilder.EnsureSchema(name: "m2");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // EF Core does not support dropping schemas via MigrationBuilder.
        // Drop manually if required: DROP SCHEMA m2 CASCADE;
    }
}
