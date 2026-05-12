# McManus — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Backend Dev
- **Joined:** 2026-05-11T02:37:43.912Z

## Learnings

<!-- Append learnings below -->

### 2026-05-12 — Backend Backlog Authoring

**Domain Insights:**
- The platform has a clear hub-and-spoke dependency structure: Epic 1 (Foundation) and Epic 9 (SAP Integration) are the critical path blockers for almost everything else. Nothing ships until auth and SAP connector are in place.
- The Promotion Discount Calculation Engine (Epic 5) is the most algorithmically complex feature; it must handle multiple concurrent promotion types, most-favorable selection logic, and coupon redemption state — all within a 200ms SLA during live POS transactions.
- The Approval Workflow Engine (Epic 2) must be designed as a generic, document-agnostic engine — the current consumer is Promotions, but the architecture should allow any document type to plug in via `document_type` + `document_id` contract.
- SAP-style authorization objects require careful upfront schema design: auth object name, field names, and valid value ranges must be defined as standing data before any permission checks can work. This is a hard dependency for Epic 1.
- Coupon issuance strategy (pre-issued on promotion activation vs. on-demand at first browse) has significant performance implications and needs a product decision before Epic 4/5 sprint planning.
- The Goods Receipt flow has a two-phase commit problem: local confirmation must be persisted before SAP posting, with a reliable retry/dead-letter queue to handle SAP unavailability.
- Attendance management is relatively simple but has an important edge case: staff who forget to clock out need a scheduled end-of-day detection job and manager notification.

**Dependencies Identified:**
- ECR vendor/SDK unknown — blocks Epic 6 detailed design.
- SMS gateway vendor unknown — blocks Epic 4 OTP implementation.
- SAP connectivity protocol (RFC/BAPI vs OData) and landscape access unknown — blocks Epic 9.
- SAP org data availability blocks Epic 2 (approval routing) and Epic 7 (attendance staff validation).

**Open Questions (carry forward to team):**
1. ECR vendor and integration protocol?
2. SMS gateway provider?
3. SAP connectivity: RFC/BAPI or OData/REST?
4. SAP auth object definitions: pre-specified by business or backend-designed?
5. Multi-store from day one or single-store MVP?
6. Coupon issuance trigger: pre-issued or on-demand?
7. Escalation target: SAP hierarchy or configurable per workflow step?
8. Data residency / sovereignty requirements?
9. Offline POS support required?
10. Return refund method: original payment only or store credit also supported?

### 2026-05-12 — Cross-Agent Context (from Initial Planning Session)

**From Keyser (Architecture) — resolves open questions:**
- **Q3 SAP connectivity answered:** ADR-006 confirms OData REST primary, NCo RFC/BAPI fallback. This unblocks Epic 9 design. SAP landscape credentials (DEV/QAS/PRD hostnames, client numbers) still needed from client.
- **Q4 SAP auth object schema:** ADR-004 positions `Platform.Authorization` as a backend-designed module. Backend team designs auth objects based on access control requirements. Confirm with business whether SAP team provides a pre-existing auth object catalogue to match.
- **Deployment confirmed:** Azure Container Apps (ACA) — Epic 9 integration tests will need ACA-accessible SAP connectivity or a stable mock/sandbox.

**From Edie (Database):**
- TenantId multi-tenancy is confirmed schema-wide. OQ-04 (multi-store) must be resolved before sprint planning — if `location_id` becomes first-class, Edie will need to add it to the schema baseline before table migrations are written.

**From Fenster (Frontend Dev):**
- Fenster's D3 (QR token) and BE-REC-001 R5 are aligned — signed JWTs with 5-minute TTL covers both backend coupon QR codes and the member-facing QR code in the Promos app. Confirm with Fenster that countdown timer design assumes 5-minute window.
- Fenster's D2 (ECR protocol) is the same as OQ-01 — both blocked until ECR vendor is confirmed by client.
