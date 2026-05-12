# Sprint Plan — M2 POS

> Generated: 2026-05-12  
> Architect: Keyser

## Overview
This sprint plan sequences foundational platform, authentication, and cross-cutting services first, followed by core business APIs and frontends. The critical path prioritizes backend infrastructure, approval/notification engines, and member/promotion flows, enabling parallel frontend and integration work once APIs stabilize. Parallelizable work includes UI polish, reporting, and non-blocking enhancements.

## Critical Path
Platform Foundation & Infrastructure → Approval Workflow Engine → Notification Service → Member Management API → Promotions API → Sales API → Attendance API → Goods Receipt API → SAP Integration Layer  
(estimated 4 sprints minimum)

Parallelisable work: Frontend app foundations, UI/UX polish, reporting, notification history, and dashboard features can proceed in parallel after Sprint 1.

## Sprint Breakdown

### Sprint 1 — Platform & Auth Foundations (Weeks 1–2)
**Goal:** All core backend infrastructure, authentication, and cross-cutting service skeletons running; basic frontend shells bootstrapped.

| # | Epic / Story | Owner Role | Critical Path? | Notes |
|---|--------------|------------|:--------------:|-------|
| 1 | Platform Foundation & Infrastructure | Backend | ✅ | Docker, Entra ID, API Key, logging, BFF routing |
| 2 | App Foundations (all 3 apps) | Frontend |  | Flutter/Blazor shells, login flows |
| 3 | SAP Connector Setup | Backend |  | Enables Epic 9 parallelization |

### Sprint 2 — Approval, Notification, Member Flows (Weeks 3–4)
**Goal:** Approval engine, notification service, and member registration/profile APIs functional; frontend registration and approval UIs demoable.

| # | Epic / Story | Owner Role | Critical Path? | Notes |
|---|--------------|------------|:--------------:|-------|
| 1 | Approval Workflow Engine | Backend | ✅ | All approval flows, audit trail |
| 2 | Notification Service | Backend | ✅ | Push, device reg, FCM/APNs |
| 3 | Member Management API | Backend | ✅ | Registration, profile, QR, lookup |
| 4 | Member Registration & Profile (FE) | Frontend |  | Registration, OTP, profile screens |
| 5 | Approval Workflow UI (Portal) | Frontend |  | Promotion approval flows |

### Sprint 3 — Promotions, Sales, Attendance (Weeks 5–6)
**Goal:** Promotions API, sales transaction, and attendance APIs; POS and member app core flows demoable end-to-end.

| # | Epic / Story | Owner Role | Critical Path? | Notes |
|---|--------------|------------|:--------------:|-------|
| 1 | Promotions API | Backend | ✅ | Formula, lifecycle, approval integration |
| 2 | Sales API | Backend | ✅ | Transaction, void, return, ECR integration |
| 3 | Attendance API | Backend | ✅ | Clock-in/out, reporting |
| 4 | Promotions, Sales, Attendance Flows (FE) | Frontend |  | POS/member app core flows |
| 5 | Promotion Formula Management (Portal) | Frontend |  | Promotion CRUD, approval |

### Sprint 4 — Goods Receipt, SAP, Polish (Weeks 7–8)
**Goal:** Goods receipt, SAP sync, and reporting; all apps feature-complete for UAT.

| # | Epic / Story | Owner Role | Critical Path? | Notes |
|---|--------------|------------|:--------------:|-------|
| 1 | Goods Receipt API | Backend | ✅ | Delivery, discrepancy, SAP posting |
| 2 | SAP Integration Layer | Backend | ✅ | Product/org sync, goods movement |
| 3 | Reporting & Dashboard | Backend |  | Sales/attendance summaries |
| 4 | Notification/Promotion History | Backend |  | Notification/coupon logs |
| 5 | UI/UX Polish & Testing | Frontend |  | Final QA, accessibility, print support |

## Risks & Assumptions
- SAP integration complexity may extend Sprint 4.
- ECR integration is deferred post-MVP; placeholder stubs in Sprint 3.
- All open questions resolved as of 2026-05-12; scope is stable.
- Parallel frontend work assumes stable API contracts by Sprint 2.
