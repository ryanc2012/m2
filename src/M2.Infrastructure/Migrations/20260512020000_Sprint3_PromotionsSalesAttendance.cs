using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M2.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Sprint3_PromotionsSalesAttendance : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── promotions ───────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "promotions",
            schema: "m2",
            columns: table => new
            {
                Id                = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId          = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId            = table.Column<Guid>(type: "uuid", nullable: false),
                Name_en           = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Name_zht          = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Type              = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Status            = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                FormulaJson       = table.Column<string>(type: "text", nullable: false),
                StartDate         = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EndDate           = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsStackable       = table.Column<bool>(type: "boolean", nullable: false),
                ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted         = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt         = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy         = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAt         = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy         = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                UpdatedAt         = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy         = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_promotions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_promotions_TenantId_ShopId_Status",
            schema: "m2",
            table: "promotions",
            columns: new[] { "TenantId", "ShopId", "Status" });

        // ── coupons ──────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "coupons",
            schema: "m2",
            columns: table => new
            {
                Id          = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId    = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId      = table.Column<Guid>(type: "uuid", nullable: false),
                PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                MemberId    = table.Column<Guid>(type: "uuid", nullable: true),
                Code        = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                IssuedAt    = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RedeemedAt  = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ExpiresAt   = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsRedeemed  = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                IsDeleted   = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt   = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy   = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAt   = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy   = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                UpdatedAt   = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy   = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_coupons", x => x.Id);
                table.ForeignKey(
                    name: "FK_coupons_promotions_PromotionId",
                    column: x => x.PromotionId,
                    principalSchema: "m2",
                    principalTable: "promotions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_coupons_Code",
            schema: "m2",
            table: "coupons",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_coupons_PromotionId_IsRedeemed",
            schema: "m2",
            table: "coupons",
            columns: new[] { "PromotionId", "IsRedeemed" });

        migrationBuilder.CreateIndex(
            name: "IX_coupons_MemberId_IsRedeemed",
            schema: "m2",
            table: "coupons",
            columns: new[] { "MemberId", "IsRedeemed" });

        // ── promotion_products ───────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "promotion_products",
            schema: "m2",
            columns: table => new
            {
                PromotionId   = table.Column<Guid>(type: "uuid", nullable: false),
                ProductId     = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                DiscountValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_promotion_products", x => new { x.PromotionId, x.ProductId });
                table.ForeignKey(
                    name: "FK_promotion_products_promotions_PromotionId",
                    column: x => x.PromotionId,
                    principalSchema: "m2",
                    principalTable: "promotions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ── sales_transactions ───────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "sales_transactions",
            schema: "m2",
            columns: table => new
            {
                Id             = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId       = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId         = table.Column<Guid>(type: "uuid", nullable: false),
                MemberId       = table.Column<Guid>(type: "uuid", nullable: true),
                CashierId      = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                TotalAmount    = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                PaymentMethod  = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Status         = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CompletedAt    = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                VoidedAt       = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                IsDeleted      = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy      = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAt      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy      = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                UpdatedAt      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy      = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sales_transactions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_sales_transactions_TenantId_ShopId_Status",
            schema: "m2",
            table: "sales_transactions",
            columns: new[] { "TenantId", "ShopId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_sales_transactions_MemberId",
            schema: "m2",
            table: "sales_transactions",
            column: "MemberId");

        // ── sales_line_items ─────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "sales_line_items",
            schema: "m2",
            columns: table => new
            {
                Id             = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId       = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId         = table.Column<Guid>(type: "uuid", nullable: false),
                TransactionId  = table.Column<Guid>(type: "uuid", nullable: false),
                ProductId      = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ProductName_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ProductName_zht = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Quantity       = table.Column<int>(type: "integer", nullable: false),
                UnitPrice      = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                LineTotal      = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                IsDeleted      = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy      = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAt      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy      = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                UpdatedAt      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy      = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sales_line_items", x => x.Id);
                table.ForeignKey(
                    name: "FK_sales_line_items_sales_transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalSchema: "m2",
                    principalTable: "sales_transactions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_sales_line_items_TransactionId",
            schema: "m2",
            table: "sales_line_items",
            column: "TransactionId");

        // ── return_transactions ──────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "return_transactions",
            schema: "m2",
            columns: table => new
            {
                Id                    = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId              = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId                = table.Column<Guid>(type: "uuid", nullable: false),
                OriginalTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                Reason                = table.Column<string>(type: "text", nullable: false),
                RefundAmount          = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                RefundMethod          = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ProcessedAt           = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                IsComplete            = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                IsDeleted             = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt             = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy             = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAt             = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy             = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                UpdatedAt             = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy             = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_return_transactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_return_transactions_sales_transactions_OriginalTransactionId",
                    column: x => x.OriginalTransactionId,
                    principalSchema: "m2",
                    principalTable: "sales_transactions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_return_transactions_OriginalTransactionId",
            schema: "m2",
            table: "return_transactions",
            column: "OriginalTransactionId");

        // ── attendance_records ───────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "attendance_records",
            schema: "m2",
            columns: table => new
            {
                Id         = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId   = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId     = table.Column<Guid>(type: "uuid", nullable: false),
                EmployeeId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                ClockInAt  = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ClockOutAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Source     = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Notes      = table.Column<string>(type: "text", nullable: true),
                IsDeleted  = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt  = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy  = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAt  = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy  = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                UpdatedAt  = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy  = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_attendance_records", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_attendance_records_TenantId_EmployeeId_ClockInAt",
            schema: "m2",
            table: "attendance_records",
            columns: new[] { "TenantId", "EmployeeId", "ClockInAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop FK dependents first
        migrationBuilder.DropTable(name: "return_transactions",  schema: "m2");
        migrationBuilder.DropTable(name: "sales_line_items",     schema: "m2");
        migrationBuilder.DropTable(name: "promotion_products",   schema: "m2");
        migrationBuilder.DropTable(name: "coupons",              schema: "m2");
        migrationBuilder.DropTable(name: "sales_transactions",   schema: "m2");
        migrationBuilder.DropTable(name: "promotions",           schema: "m2");
        migrationBuilder.DropTable(name: "attendance_records",   schema: "m2");
    }
}
