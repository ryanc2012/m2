using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4_IdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "m2",
                table: "sales_transactions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UIX_sales_transactions_TenantId_ShopId_IdempotencyKey",
                schema: "m2",
                table: "sales_transactions",
                columns: new[] { "TenantId", "ShopId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UIX_sales_transactions_TenantId_ShopId_IdempotencyKey",
                schema: "m2",
                table: "sales_transactions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "m2",
                table: "sales_transactions");
        }
    }
}
