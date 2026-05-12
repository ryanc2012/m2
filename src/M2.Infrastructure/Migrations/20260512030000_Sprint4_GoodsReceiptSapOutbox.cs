using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M2.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Sprint4_GoodsReceiptSapOutbox : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── goods_receipt_notes ───────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "goods_receipt_notes",
            schema: "m2",
            columns: table => new
            {
                Id                    = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId              = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId                = table.Column<Guid>(type: "uuid", nullable: false),
                SapDeliveryNoteNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Status                = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ReceivedAt            = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ConfirmedAt           = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                table.PrimaryKey("PK_goods_receipt_notes", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_goods_receipt_notes_TenantId_ShopId_IsDeleted",
            schema: "m2",
            table: "goods_receipt_notes",
            columns: new[] { "TenantId", "ShopId", "IsDeleted" });

        migrationBuilder.CreateIndex(
            name: "IX_goods_receipt_notes_TenantId_ShopId_Status",
            schema: "m2",
            table: "goods_receipt_notes",
            columns: new[] { "TenantId", "ShopId", "Status" });

        // ── goods_receipt_line_items ──────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "goods_receipt_line_items",
            schema: "m2",
            columns: table => new
            {
                Id                   = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId             = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId               = table.Column<Guid>(type: "uuid", nullable: false),
                GoodsReceiptNoteId   = table.Column<Guid>(type: "uuid", nullable: false),
                ProductCode          = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ProductName_en       = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ProductName_zht      = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ExpectedQty          = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                ReceivedQty          = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                UnitOfMeasure        = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                DiscrepancyNote      = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsDeleted            = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt            = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy            = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAt            = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy            = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                UpdatedAt            = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy            = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_goods_receipt_line_items", x => x.Id);
                table.ForeignKey(
                    name: "FK_goods_receipt_line_items_goods_receipt_notes_GoodsReceiptNoteId",
                    column: x => x.GoodsReceiptNoteId,
                    principalSchema: "m2",
                    principalTable: "goods_receipt_notes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_goods_receipt_line_items_GoodsReceiptNoteId",
            schema: "m2",
            table: "goods_receipt_line_items",
            column: "GoodsReceiptNoteId");

        // ── sap_outbox_entries ────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "sap_outbox_entries",
            schema: "m2",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId     = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId       = table.Column<Guid>(type: "uuid", nullable: false),
                Operation    = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Payload      = table.Column<string>(type: "text", nullable: false),
                Status       = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ProcessedAt  = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                RetryCount   = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                IsDeleted    = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt    = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy    = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAt    = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy    = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                UpdatedAt    = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy    = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sap_outbox_entries", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_sap_outbox_entries_TenantId_Status",
            schema: "m2",
            table: "sap_outbox_entries",
            columns: new[] { "TenantId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_sap_outbox_entries_Status_CreatedAt",
            schema: "m2",
            table: "sap_outbox_entries",
            columns: new[] { "Status", "CreatedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "sap_outbox_entries",          schema: "m2");
        migrationBuilder.DropTable(name: "goods_receipt_line_items",    schema: "m2");
        migrationBuilder.DropTable(name: "goods_receipt_notes",         schema: "m2");
    }
}
