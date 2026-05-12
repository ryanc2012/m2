using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace M2.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Sprint2_MembersApprovalsNotifications : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── members ──────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "members",
            schema: "m2",
            columns: table => new
            {
                Id             = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId       = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId         = table.Column<Guid>(type: "uuid", nullable: false),
                FirstName_en   = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                FirstName_zht  = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                LastName_en    = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                LastName_zht   = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Phone          = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Email          = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                QrCode         = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                MembershipTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                JoinedAt       = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsActive       = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                table.PrimaryKey("PK_members", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_members_TenantId_Phone",
            schema: "m2",
            table: "members",
            columns: new[] { "TenantId", "Phone" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_members_QrCode",
            schema: "m2",
            table: "members",
            column: "QrCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_members_TenantId_ShopId",
            schema: "m2",
            table: "members",
            columns: new[] { "TenantId", "ShopId" });

        // ── otp_requests ─────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "otp_requests",
            schema: "m2",
            columns: table => new
            {
                Id        = table.Column<Guid>(type: "uuid", nullable: false),
                MemberId  = table.Column<Guid>(type: "uuid", nullable: false),
                Code      = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsUsed    = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_otp_requests", x => x.Id);
                table.ForeignKey(
                    name: "FK_otp_requests_members_MemberId",
                    column: x => x.MemberId,
                    principalSchema: "m2",
                    principalTable: "members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_otp_requests_MemberId_IsUsed",
            schema: "m2",
            table: "otp_requests",
            columns: new[] { "MemberId", "IsUsed" });

        // ── approval_requests ────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "approval_requests",
            schema: "m2",
            columns: table => new
            {
                Id          = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId    = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId      = table.Column<Guid>(type: "uuid", nullable: false),
                EntityType  = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                EntityId    = table.Column<Guid>(type: "uuid", nullable: false),
                Status      = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CurrentStep = table.Column<int>(type: "integer", nullable: false),
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
                table.PrimaryKey("PK_approval_requests", x => x.Id);
            });

        // ── approval_steps ───────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "approval_steps",
            schema: "m2",
            columns: table => new
            {
                Id                = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId          = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId            = table.Column<Guid>(type: "uuid", nullable: false),
                RequestId         = table.Column<Guid>(type: "uuid", nullable: false),
                StepNumber        = table.Column<int>(type: "integer", nullable: false),
                ApproverId        = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                ApproverType      = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Status            = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Comment           = table.Column<string>(type: "text", nullable: true),
                ActedAt           = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                table.PrimaryKey("PK_approval_steps", x => x.Id);
                table.ForeignKey(
                    name: "FK_approval_steps_approval_requests_RequestId",
                    column: x => x.RequestId,
                    principalSchema: "m2",
                    principalTable: "approval_requests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_approval_steps_RequestId",
            schema: "m2",
            table: "approval_steps",
            column: "RequestId");

        // ── approval_policies ────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "approval_policies",
            schema: "m2",
            columns: table => new
            {
                Id         = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId   = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId     = table.Column<Guid>(type: "uuid", nullable: false),
                EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Mode       = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                MaxLevels  = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
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
                table.PrimaryKey("PK_approval_policies", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_approval_policies_TenantId_EntityType",
            schema: "m2",
            table: "approval_policies",
            columns: new[] { "TenantId", "EntityType" },
            unique: true);

        // ── notification_templates ───────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "notification_templates",
            schema: "m2",
            columns: table => new
            {
                Id        = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId  = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId    = table.Column<Guid>(type: "uuid", nullable: false),
                Type      = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Title_en  = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Title_zht = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Body_en   = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Body_zht  = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notification_templates", x => x.Id);
            });

        // ── device_registrations ─────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "device_registrations",
            schema: "m2",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId     = table.Column<Guid>(type: "uuid", nullable: false),
                ShopId       = table.Column<Guid>(type: "uuid", nullable: false),
                UserId       = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Platform     = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                FcmToken     = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                ApnsToken    = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                table.PrimaryKey("PK_device_registrations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_device_registrations_TenantId_UserId_Platform",
            schema: "m2",
            table: "device_registrations",
            columns: new[] { "TenantId", "UserId", "Platform" },
            unique: true);

        // ── notification_logs ────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "notification_logs",
            schema: "m2",
            columns: table => new
            {
                Id                     = table.Column<Guid>(type: "uuid", nullable: false),
                NotificationTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                RecipientUserId        = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                SentAt                 = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Status                 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ErrorMessage           = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notification_logs", x => x.Id);
                table.ForeignKey(
                    name: "FK_notification_logs_notification_templates_NotificationTemplateId",
                    column: x => x.NotificationTemplateId,
                    principalSchema: "m2",
                    principalTable: "notification_templates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_notification_logs_NotificationTemplateId",
            schema: "m2",
            table: "notification_logs",
            column: "NotificationTemplateId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "otp_requests",          schema: "m2");
        migrationBuilder.DropTable(name: "notification_logs",     schema: "m2");
        migrationBuilder.DropTable(name: "approval_steps",        schema: "m2");
        migrationBuilder.DropTable(name: "members",               schema: "m2");
        migrationBuilder.DropTable(name: "notification_templates", schema: "m2");
        migrationBuilder.DropTable(name: "approval_requests",     schema: "m2");
        migrationBuilder.DropTable(name: "approval_policies",     schema: "m2");
        migrationBuilder.DropTable(name: "device_registrations",  schema: "m2");
    }
}
