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
