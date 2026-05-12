# POS System Data Design

**Date:** 2026-05-12
**Author:** Edie (Database Specialist)

---

## 1. Database Technology Recommendation

**Recommended:** PostgreSQL

**Rationale:**
- Open-source, robust, and widely supported for enterprise workloads
- Advanced features (JSONB, partitioning, full-text search)
- Strong .NET support (Npgsql)
- Lower TCO than SQL Server/Azure SQL

**Database per Service vs. Shared:**
- **Recommendation:** Shared database for core domains, with clear schema boundaries. Separate DB for integration-heavy or high-volume services if needed.

---

## 2. Domain Entity Model

### Member Domain
- **Member** (MemberId, Name, MobilePhone, Email, StatusId, RegisteredAt)
- **MemberStatus** (StatusId, Name, IsActive)
- **OTPVerification** (OtpId, MemberId, Code, ExpiresAt, VerifiedAt)
- **QRCoupon** (CouponId, MemberId, Code, Status, IssuedAt, RedeemedAt)

### Promotion Domain
- **Promotion** (PromotionId, Name, StatusId, StartDate, EndDate)
- **PromotionFormula** (FormulaId, PromotionId, FormulaText, DiscountTypeId)
- **DiscountType** (DiscountTypeId, Name)
- **PromotionStatus** (StatusId, Name)
- **PromotionApplicabilityRule** (RuleId, PromotionId, RuleText)

### Sales Domain
- **SalesTransaction** (TransactionId, StoreId, MemberId, Date, TotalAmount, Status)
- **SalesTransactionLine** (LineId, TransactionId, ProductId, Qty, Price, Discount)
- **SalesVoid** (VoidId, TransactionId, Reason, VoidedAt)
- **SalesReturn** (ReturnId, TransactionId, Date, Reason)
- **SalesReturnLine** (ReturnLineId, ReturnId, ProductId, Qty, Amount)

### Attendance Domain
- **StaffAttendance** (AttendanceId, StaffId, Date, Status)
- **ClockEvent** (EventId, AttendanceId, EventType, EventTime)

### Goods Receipt Domain
- **ReplenishmentDelivery** (DeliveryId, WarehouseId, StoreId, DeliveryDate, Status)
- **DeliveryLine** (LineId, DeliveryId, ProductId, Qty)
- **GoodsReceipt** (ReceiptId, DeliveryId, ReceivedAt, Status)
- **GoodsReceiptLine** (LineId, ReceiptId, ProductId, Qty)

### Approval Domain
- **ApprovalWorkflow** (WorkflowId, Name, EntityType)
- **ApprovalStep** (StepId, WorkflowId, StepOrder, ApproverRole)
- **ApprovalTask** (TaskId, WorkflowId, EntityId, Status, AssignedTo, DueDate)
- **ApprovalHistory** (HistoryId, TaskId, Action, PerformedBy, PerformedAt)

### Authorization Domain
- **AuthorizationObject** (ObjectId, Name, Description)
- **AuthorizationField** (FieldId, ObjectId, Name, Value)
- **UserAuthorization** (UserAuthId, UserId, ObjectId, FieldId, Value)

### Notification Domain
- **DeviceRegistration** (DeviceId, UserId, DeviceToken, RegisteredAt)
- **Notification** (NotificationId, Title, Body, CreatedAt)
- **NotificationDelivery** (DeliveryId, NotificationId, DeviceId, DeliveredAt, Status)

---

## 3. Key Table Schemas

### Member
```sql
CREATE TABLE Member (
    MemberId UUID PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    MobilePhone VARCHAR(20) NOT NULL,
    Email VARCHAR(100),
    StatusId INT NOT NULL REFERENCES MemberStatus(StatusId),
    RegisteredAt TIMESTAMP NOT NULL,
    TenantId UUID NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    CreatedBy VARCHAR(50) NOT NULL,
    UpdatedAt TIMESTAMP,
    UpdatedBy VARCHAR(50),
    IsDeleted BOOLEAN DEFAULT FALSE,
    DeletedAt TIMESTAMP,
    UNIQUE(MobilePhone, TenantId),
    INDEX idx_member_phone (MobilePhone),
    INDEX idx_member_tenant (TenantId)
);
```

### Promotion
```sql
CREATE TABLE Promotion (
    PromotionId UUID PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    StatusId INT NOT NULL REFERENCES PromotionStatus(StatusId),
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    TenantId UUID NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    CreatedBy VARCHAR(50) NOT NULL,
    UpdatedAt TIMESTAMP,
    UpdatedBy VARCHAR(50),
    IsDeleted BOOLEAN DEFAULT FALSE,
    DeletedAt TIMESTAMP,
    INDEX idx_promotion_active (StatusId, StartDate, EndDate),
    INDEX idx_promotion_tenant (TenantId)
);
```

### SalesTransaction
```sql
CREATE TABLE SalesTransaction (
    TransactionId UUID PRIMARY KEY,
    StoreId UUID NOT NULL,
    MemberId UUID,
    Date TIMESTAMP NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status VARCHAR(20) NOT NULL,
    TenantId UUID NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    CreatedBy VARCHAR(50) NOT NULL,
    UpdatedAt TIMESTAMP,
    UpdatedBy VARCHAR(50),
    IsDeleted BOOLEAN DEFAULT FALSE,
    DeletedAt TIMESTAMP,
    INDEX idx_sales_txn_store_date (StoreId, Date),
    INDEX idx_sales_txn_member (MemberId),
    INDEX idx_sales_txn_tenant (TenantId)
);
```

### StaffAttendance
```sql
CREATE TABLE StaffAttendance (
    AttendanceId UUID PRIMARY KEY,
    StaffId UUID NOT NULL,
    Date DATE NOT NULL,
    Status VARCHAR(20) NOT NULL,
    TenantId UUID NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    CreatedBy VARCHAR(50) NOT NULL,
    UpdatedAt TIMESTAMP,
    UpdatedBy VARCHAR(50),
    IsDeleted BOOLEAN DEFAULT FALSE,
    DeletedAt TIMESTAMP,
    INDEX idx_attendance_staff_date (StaffId, Date),
    INDEX idx_attendance_tenant (TenantId)
);
```

### GoodsReceipt
```sql
CREATE TABLE GoodsReceipt (
    ReceiptId UUID PRIMARY KEY,
    DeliveryId UUID NOT NULL,
    ReceivedAt TIMESTAMP NOT NULL,
    Status VARCHAR(20) NOT NULL,
    TenantId UUID NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    CreatedBy VARCHAR(50) NOT NULL,
    UpdatedAt TIMESTAMP,
    UpdatedBy VARCHAR(50),
    IsDeleted BOOLEAN DEFAULT FALSE,
    DeletedAt TIMESTAMP,
    INDEX idx_goods_receipt_delivery (DeliveryId),
    INDEX idx_goods_receipt_tenant (TenantId)
);
```

---

## 4. SAP Integration Data Considerations

- **FROM SAP:** Products, staff, org structure (stores, warehouses)
- **TO SAP:** Goods movements (goods receipt, delivery), financial postings (sales, returns)
- **Sync Strategy:** Prefer near real-time for critical data (products, staff), batch for bulk movements/financials. Use staging tables for inbound/outbound SAP data to decouple sync and processing.

---

## 5. Multi-tenancy Consideration

- **Approach:** TenantId column in all tables. Enforced via application logic and filtered indexes. Separate schemas/databases only if required for isolation or scale.

---

## 6. Indexing Strategy

- **Member lookup by phone:** idx_member_phone (MobilePhone)
- **Active promotions:** idx_promotion_active (StatusId, StartDate, EndDate)
- **Sales transaction lookup:** idx_sales_txn_store_date (StoreId, Date), idx_sales_txn_member (MemberId)
- **Attendance by staff/date:** idx_attendance_staff_date (StaffId, Date)

---

## 7. Migration Strategy

- **Tool:** EF Core migrations (for .NET stack)
- **Naming Convention:** yyyyMMddHHmmss_Description.sql (e.g., 20260512173000_AddMemberTable.sql)
- **Promotion:** Dev → Test → UAT → Prod, with migration scripts versioned and peer-reviewed.

---

## Sprint 1 Foundation

### Schema
- Default schema: `m2`
- All tables use `TenantId` (Guid) + `ShopId` (Guid) as mandatory discriminators
- Soft delete via `IsDeleted` (bool)
- Bilingual text stored as `{property}_en` / `{property}_zht` column pairs

### Pending tables (Sprint 2+)
- ~~Members (Sprint 2)~~ — **delivered**
- Promotions (Sprint 3)
- Sales (Sprint 3)
- Attendance (Sprint 3)
- GoodsReceipts (Sprint 4)

---

## Sprint 2 Tables — Members, Approvals, Notifications

Migration: `20260512010000_Sprint2_MembersApprovalsNotifications`

### members

| Column | Type | Notes |
|--------|------|-------|
| Id | uuid PK | Client-assigned GUID |
| TenantId | uuid NOT NULL | Multi-tenancy discriminator |
| ShopId | uuid NOT NULL | Multi-store discriminator |
| FirstName_en | varchar(500) NOT NULL | Bilingual owned value |
| FirstName_zht | varchar(500) NOT NULL | Bilingual owned value |
| LastName_en | varchar(500) NOT NULL | Bilingual owned value |
| LastName_zht | varchar(500) NOT NULL | Bilingual owned value |
| Phone | varchar(20) NOT NULL | |
| Email | varchar(256) NULL | |
| QrCode | varchar(100) NOT NULL | Global unique |
| MembershipTier | varchar(50) NULL | |
| JoinedAt | timestamptz NOT NULL | |
| IsActive | boolean NOT NULL | Default true |
| IsDeleted | boolean NOT NULL | Default false (soft delete) |
| DeletedAt | timestamptz NULL | |
| DeletedBy | varchar(256) NULL | |
| CreatedAt | timestamptz NOT NULL | |
| CreatedBy | varchar(256) NULL | |
| UpdatedAt | timestamptz NULL | |
| UpdatedBy | varchar(256) NULL | |

**Indexes:** UNIQUE (TenantId, Phone) · UNIQUE (QrCode) · (TenantId, ShopId)

### otp_requests

| Column | Type | Notes |
|--------|------|-------|
| Id | uuid PK | |
| MemberId | uuid NOT NULL FK → members | Cascade delete |
| Code | varchar(6) NOT NULL | |
| ExpiresAt | timestamptz NOT NULL | |
| IsUsed | boolean NOT NULL | |
| CreatedAt | timestamptz NOT NULL | |

**Indexes:** (MemberId, IsUsed)

### approval_requests

| Column | Type | Notes |
|--------|------|-------|
| Id | uuid PK | |
| TenantId | uuid NOT NULL | |
| ShopId | uuid NOT NULL | |
| EntityType | varchar(100) NOT NULL | e.g. "GoodsReceipt" |
| EntityId | uuid NOT NULL | FK into the entity's own table |
| Status | varchar(50) NOT NULL | Enum: Pending/Approved/Rejected/Escalated |
| CurrentStep | int NOT NULL | |
| + audit/soft-delete columns | | Standard BaseEntity columns |

### approval_steps

| Column | Type | Notes |
|--------|------|-------|
| Id | uuid PK | |
| TenantId | uuid NOT NULL | |
| ShopId | uuid NOT NULL | |
| RequestId | uuid NOT NULL FK → approval_requests | Cascade delete |
| StepNumber | int NOT NULL | |
| ApproverId | varchar(256) NOT NULL | SAP position/HCM node |
| ApproverType | varchar(50) NOT NULL | Enum: SapPosition/SapHcm |
| Status | varchar(50) NOT NULL | Enum: Pending/Approved/Rejected/Escalated |
| Comment | text NULL | |
| ActedAt | timestamptz NULL | |
| + audit/soft-delete columns | | Standard BaseEntity columns |

**Indexes:** (RequestId)

### approval_policies

| Column | Type | Notes |
|--------|------|-------|
| Id | uuid PK | |
| TenantId | uuid NOT NULL | |
| ShopId | uuid NOT NULL | |
| EntityType | varchar(100) NOT NULL | |
| Mode | varchar(50) NOT NULL | Enum: SapHcmHierarchy/StepByStepPosition |
| MaxLevels | int NOT NULL | Default 2 (N-level chain, ADR-021) |
| + audit/soft-delete columns | | Standard BaseEntity columns |

**Indexes:** UNIQUE (TenantId, EntityType)

### notification_templates

| Column | Type | Notes |
|--------|------|-------|
| Id | uuid PK | |
| TenantId | uuid NOT NULL | |
| ShopId | uuid NOT NULL | |
| Type | varchar(100) NOT NULL | e.g. "OtpVerification" |
| Title_en | varchar(500) NOT NULL | Bilingual owned value |
| Title_zht | varchar(500) NOT NULL | Bilingual owned value |
| Body_en | varchar(500) NOT NULL | Bilingual owned value |
| Body_zht | varchar(500) NOT NULL | Bilingual owned value |
| + audit/soft-delete columns | | Standard BaseEntity columns |

### device_registrations

| Column | Type | Notes |
|--------|------|-------|
| Id | uuid PK | |
| TenantId | uuid NOT NULL | |
| ShopId | uuid NOT NULL | |
| UserId | varchar(256) NOT NULL | Entra ID subject |
| Platform | varchar(20) NOT NULL | "ios" / "android" |
| FcmToken | varchar(512) NULL | Firebase Cloud Messaging |
| ApnsToken | varchar(512) NULL | Apple Push Notification |
| RegisteredAt | timestamptz NOT NULL | |
| + audit/soft-delete columns | | Standard BaseEntity columns |

**Indexes:** UNIQUE (TenantId, UserId, Platform)

### notification_logs

| Column | Type | Notes |
|--------|------|-------|
| Id | uuid PK | |
| NotificationTemplateId | uuid NOT NULL FK → notification_templates | Restrict delete |
| RecipientUserId | varchar(256) NOT NULL | |
| SentAt | timestamptz NOT NULL | |
| Status | varchar(50) NOT NULL | |
| ErrorMessage | text NULL | |

**Indexes:** (NotificationTemplateId)


