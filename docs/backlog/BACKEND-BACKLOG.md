# Backend Product Backlog

**Project:** Meka POS Platform  
**Author:** McManus (Backend Dev)  
**Date:** 2026-05-12  
**Status:** Draft — Ready for Sprint Planning

---

## Table of Contents

1. [Epic 1: Platform Foundation & Infrastructure](#epic-1-platform-foundation--infrastructure)
2. [Epic 2: Cross-Cutting — Approval Workflow Engine](#epic-2-cross-cutting--approval-workflow-engine)
3. [Epic 3: Cross-Cutting — Notification Service](#epic-3-cross-cutting--notification-service)
4. [Epic 4: Member Management API](#epic-4-member-management-api)
5. [Epic 5: Promotions API](#epic-5-promotions-api)
6. [Epic 6: Sales API](#epic-6-sales-api)
7. [Epic 7: Attendance API](#epic-7-attendance-api)
8. [Epic 8: Goods Receipt API](#epic-8-goods-receipt-api)
9. [Epic 9: SAP Integration Layer](#epic-9-sap-integration-layer)

---

## Epic 1: Platform Foundation & Infrastructure

**Description:** Establish the core infrastructure, authentication/authorization framework, observability stack, and shared service contracts that all other epics depend on. Nothing else ships without this.  
**Priority:** Must Have  
**Estimated Effort:** XL

---

### Feature: Container Infrastructure

**Description:** Docker and Docker Compose setup for all services with proper health checks, environment configuration, and local development parity with production.

#### Story: As a developer, I want all services containerized with Docker so that the environment is consistent across local, CI, and production

**Acceptance Criteria:**
- [ ] Given the repository, when `docker compose up` is run, then all services start successfully without manual configuration
- [ ] Given a running container, when the `/health` endpoint is polled, then it returns `200 OK` with service status
- [ ] Given any container, when it fails its health check 3 consecutive times, then Docker marks it unhealthy and restarts it
- [ ] Given environment configuration, when a service starts, then it reads all secrets from environment variables (no hardcoded values)
- [ ] Given the Docker Compose file, when reviewed, then each service declares resource limits (CPU, memory)
- [ ] Given a CI pipeline, when triggered, then Docker images are built, tagged with commit SHA, and pushed to the container registry

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** None

---

### Feature: Azure Entra ID Authentication Middleware

**Description:** Middleware that validates Azure Entra ID JWT tokens on all protected API endpoints, extracting claims (user ID, roles, groups) into the request context.

#### Story: As an internal user, I want my Azure Entra ID token to authenticate me to the API so that I do not need a separate login

**Acceptance Criteria:**
- [ ] Given a valid Entra ID JWT, when a request is made to a protected endpoint, then the request is allowed and user claims are available in context
- [ ] Given an expired JWT, when a request is made, then the API returns `401 Unauthorized` with a `WWW-Authenticate` header
- [ ] Given a JWT with an invalid signature, when a request is made, then the API returns `401 Unauthorized`
- [ ] Given a valid JWT but insufficient role claims, when a request is made to a role-restricted endpoint, then the API returns `403 Forbidden`
- [ ] Given a public endpoint (e.g., `GET /health`), when accessed without a token, then the request is allowed
- [ ] Given token validation, when JWKS keys are fetched from Entra ID, then keys are cached and refreshed on rotation

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** None

#### Story: As a system administrator, I want token validation configuration to be environment-driven so that the same image works across environments

**Acceptance Criteria:**
- [ ] Given environment variables `ENTRA_TENANT_ID`, `ENTRA_CLIENT_ID`, `ENTRA_AUDIENCE`, when the service starts, then token validation uses these values
- [ ] Given missing required Entra ID environment variables, when the service starts, then it fails fast with a clear error message
- [ ] Given a staging environment, when configured with a staging Entra app registration, then only staging-issued tokens are accepted

**Priority:** Must Have  
**Complexity:** S  
**Dependencies:** Container Infrastructure

---

### Feature: API Key Management

**Description:** Issue, store, validate, and rotate API keys for system-to-system and public app consumers (e.g., Meka Promotion App calling the public promotions endpoint).

#### Story: As a system administrator, I want to issue API keys to consumers so that external systems can authenticate without Azure Entra ID

**Acceptance Criteria:**
- [ ] Given a create-key request with `consumer_name`, `scopes[]`, and optional `expiry`, when submitted by an admin, then a unique API key is generated and returned once (plaintext, never stored)
- [ ] Given an issued API key, when stored in the database, then only the SHA-256 hash is persisted alongside metadata (consumer name, scopes, expiry, created_at, last_used_at)
- [ ] Given a list-keys request, when called by an admin, then all keys are returned with metadata but never the plaintext value
- [ ] Given a revoke-key request by an admin, when submitted, then the key is immediately invalidated and all subsequent requests using it return `401`

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Azure Entra ID Authentication (admin endpoints must be Entra-protected)

#### Story: As a consumer, I want my API key validated on every request so that unauthorized access is blocked

**Acceptance Criteria:**
- [ ] Given a valid API key in the `X-API-Key` header, when a request is made to a key-protected endpoint, then the request is allowed and consumer identity is available in context
- [ ] Given an invalid or revoked API key, when a request is made, then the API returns `401 Unauthorized`
- [ ] Given a valid API key without the required scope, when accessing a scope-restricted endpoint, then the API returns `403 Forbidden`
- [ ] Given any authenticated request, when processed, then `last_used_at` is updated asynchronously (non-blocking)
- [ ] Given an expired API key, when a request is made, then the API returns `401 Unauthorized` with an `X-Key-Expired` response header

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** API Key issuance story

#### Story: As a system administrator, I want to rotate API keys so that compromised keys can be replaced without consumer downtime

**Acceptance Criteria:**
- [ ] Given a rotate-key request, when submitted, then a new key is generated and a grace period is set during which both old and new keys are valid
- [ ] Given a grace period has elapsed, when the old key is used, then it returns `401 Unauthorized`
- [ ] Given key rotation, when completed, then an audit log entry is created with admin identity and timestamp

**Priority:** Should Have  
**Complexity:** M  
**Dependencies:** API Key issuance story

---

### Feature: SAP-Style Authorization Service

**Description:** Shared authorization service implementing SAP-style authorization objects (auth object name → field name → field value). All services call this to check user permissions against business objects.

#### Story: As a developer, I want a centralized authorization service so that permission logic is not duplicated across APIs

**Acceptance Criteria:**
- [ ] Given an auth check request with `(user_id, auth_object, field_name, field_value)`, when evaluated, then the service returns `allow` or `deny` based on the user's assigned authorization profiles
- [ ] Given an authorization object definition, when created, then it specifies the object name, allowed field names, and allowed value ranges/sets per field
- [ ] Given a user with multiple authorization profiles, when evaluated, then the most permissive applicable profile is used (SAP OR semantics across profiles)
- [ ] Given a wildcard field value `*` in an authorization profile, when evaluated against any field value, then it matches
- [ ] Given an auth check, when the authorization service is unavailable, then a `503` is returned and access is denied (fail-closed)

**Priority:** Must Have  
**Complexity:** L  
**Dependencies:** Azure Entra ID Authentication

#### Story: As a system administrator, I want to define and assign authorization profiles to users so that I can control access to business objects

**Acceptance Criteria:**
- [ ] Given a create-profile request with a name and a list of auth object assignments, when submitted, then the profile is persisted
- [ ] Given an assign-profile request with `(user_id, profile_id)`, when submitted, then the user's profile assignment is updated
- [ ] Given a user profile assignment change, when applied, then it takes effect within 30 seconds (cache TTL or push invalidation)
- [ ] Given a list-profiles request, when called, then all profiles with their auth object assignments are returned

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Authorization object definition story

---

### Feature: Logging and Observability

**Description:** Structured logging, distributed tracing, and metrics collection across all services.

#### Story: As an operator, I want structured logs from all services so that I can query and correlate events in production

**Acceptance Criteria:**
- [ ] Given any service log event, when emitted, then it is structured JSON with `timestamp`, `level`, `service`, `trace_id`, `span_id`, `message`, and optional `fields`
- [ ] Given an incoming HTTP request, when processed, then a `trace_id` is generated (or propagated from `traceparent` header) and attached to all downstream calls
- [ ] Given a log at `ERROR` level or above, when emitted, then it includes the full stack trace and request context
- [ ] Given a request to any API, when completed, then an access log entry is written with `method`, `path`, `status_code`, `duration_ms`, and `consumer_identity`
- [ ] Given the observability stack, when deployed, then logs are collected in a centralized store queryable by `trace_id`

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Container Infrastructure

#### Story: As an operator, I want service health metrics exposed so that I can alert on degradation

**Acceptance Criteria:**
- [ ] Given a running service, when `/metrics` is polled, then Prometheus-compatible metrics are returned including request count, error rate, and p99 latency
- [ ] Given error rate exceeding 5% in a 1-minute window, when detected, then an alert is triggerable via the metrics system
- [ ] Given a downstream dependency (SAP, DB, notification service), when its health is checked, then the result is included in the service's own health endpoint response

**Priority:** Should Have  
**Complexity:** M  
**Dependencies:** Logging story

---

### Feature: BFF API Gateway / Routing Layer

**Description:** Backend for Frontend routing layer that aggregates and adapts responses for each front-end client (Meka Promotion App, Meka POS, M2 Portal).

#### Story: As a front-end client, I want a single entry-point API so that I do not need to call multiple microservices directly

**Acceptance Criteria:**
- [ ] Given a request from the Meka Promotion App, when routed through the BFF, then only endpoints relevant to that client are exposed
- [ ] Given a request from the Meka POS System, when routed through the BFF, then sales, attendance, and goods receipt endpoints are accessible
- [ ] Given a request from M2 Portal, when routed through the BFF, then promotion management and approval endpoints are accessible
- [ ] Given any BFF route, when authentication fails, then the BFF returns `401` before proxying downstream
- [ ] Given rate limiting configuration, when a consumer exceeds the limit, then the BFF returns `429 Too Many Requests` with `Retry-After` header

**Priority:** Must Have  
**Complexity:** L  
**Dependencies:** Entra ID middleware, API Key management

---

### Feature: Shared Error Handling and Response Envelope

**Description:** Standardized response envelope and error format used consistently across all APIs.

#### Story: As a front-end developer, I want all API responses to use a consistent envelope so that I can handle success and error cases uniformly

**Acceptance Criteria:**
- [ ] Given a successful response, when returned, then it uses the envelope `{ "success": true, "data": {...}, "meta": {...} }`
- [ ] Given an error response, when returned, then it uses `{ "success": false, "error": { "code": "ERROR_CODE", "message": "...", "details": [...] } }`
- [ ] Given a validation error (`400`), when returned, then `details` contains per-field error messages
- [ ] Given an unhandled exception in any service, when it propagates, then it is caught by global middleware, logged with `trace_id`, and returned as a sanitized `500` response (no stack trace exposed to client)
- [ ] Given any API response, when it includes pagination, then `meta` contains `{ "page": N, "page_size": N, "total": N, "total_pages": N }`

**Priority:** Must Have  
**Complexity:** S  
**Dependencies:** None

---

## Epic 2: Cross-Cutting — Approval Workflow Engine

**Description:** A reusable, position-based sequential approval engine. Supports any approvable business object (promotions, purchase orders, etc.) by referencing it generically. Approval sequences are driven by organizational positions pulled from SAP master data.  
**Priority:** Must Have  
**Estimated Effort:** XL

---

### Feature: Workflow Definition

**Description:** Define approval workflow templates: which positions approve, in what sequence, and under what conditions.

#### Story: As a system administrator, I want to define approval workflow templates so that business rules for approval sequences are captured centrally

**Acceptance Criteria:**
- [ ] Given a create-workflow request with `name`, `applicable_document_type`, and ordered `steps[]` (each step: `position_id`, `action_required`, optional `condition`), when submitted, then the workflow template is persisted
- [ ] Given a workflow template, when it has multiple steps, then steps are ordered by `sequence_number` and each step must be completed before the next is activated
- [ ] Given a workflow condition (e.g., `amount > 10000`), when evaluated against a submitted document, then the step is only created if the condition is met
- [ ] Given a list-workflow-templates request, when called, then all templates with their steps are returned
- [ ] Given a workflow template in use by pending approval tasks, when deletion is attempted, then it is rejected with a `409 Conflict`

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** SAP organizational data (positions) from Epic 9

---

### Feature: Submit for Approval

**Description:** Any service can submit a document for approval by referencing a workflow template. The engine instantiates the workflow and creates the first approval task.

#### Story: As a business user, I want to submit a document for approval so that it enters the defined review process

**Acceptance Criteria:**
- [ ] Given a submit-approval request with `document_type`, `document_id`, `workflow_template_id`, and `submitted_by`, when submitted, then a workflow instance is created with status `IN_PROGRESS` and the first pending task is created
- [ ] Given a workflow instance created, when the first task is assigned, then the approver in the corresponding position is notified (trigger to Notification Service)
- [ ] Given a document already in an active workflow instance, when a second submit is attempted, then it is rejected with `409 Conflict`
- [ ] Given a submitted document, when the workflow has a conditional first step that evaluates to false, then the engine skips to the next applicable step

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Workflow Definition, Notification Service (Epic 3)

---

### Feature: Approval Task Routing

**Description:** Route tasks to the correct approver based on position. Manage task state transitions.

#### Story: As an approver, I want to see my pending approval tasks so that I know what requires my action

**Acceptance Criteria:**
- [ ] Given a user who holds a position that matches an active approval task, when they call `GET /approval/tasks?mine=true`, then all tasks assigned to their position are returned
- [ ] Given an approval task, when returned, then it includes `document_type`, `document_id`, `submitted_by`, `submitted_at`, `due_at`, and `workflow_step` details
- [ ] Given an approval task, when the assignee position has multiple holders, then any one holder can act (first-to-act semantics)
- [ ] Given completed step N, when the engine advances, then it creates a task for step N+1 and notifies the new approver

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Submit for Approval, SAP organizational data

---

### Feature: Approve / Reject

**Description:** Approvers can approve or reject tasks with mandatory comments on rejection.

#### Story: As an approver, I want to approve or reject a task so that the workflow progresses or is halted

**Acceptance Criteria:**
- [ ] Given an approver acting on their assigned task with action `APPROVE`, when submitted, then the task is marked `APPROVED`, the audit record is written, and the workflow advances to the next step
- [ ] Given an approver acting on their assigned task with action `REJECT`, when submitted without a comment, then it is rejected with `400 Bad Request` (comment is mandatory on rejection)
- [ ] Given an approver acting on their assigned task with action `REJECT` and a comment, when submitted, then the task is marked `REJECTED`, the workflow instance is marked `REJECTED`, and the document submitter is notified
- [ ] Given the final step of a workflow being approved, when submitted, then the workflow instance is marked `APPROVED` and the submitter is notified
- [ ] Given a task the user does not own, when they attempt to approve/reject, then `403 Forbidden` is returned

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Approval Task Routing

---

### Feature: Escalation

**Description:** Automatically escalate overdue approval tasks to the next level or a designated escalation contact.

#### Story: As a system, I want to escalate overdue approval tasks so that workflows do not stall indefinitely

**Acceptance Criteria:**
- [ ] Given an approval task with a `due_at` timestamp, when the current time exceeds `due_at` and the task is still pending, then the escalation job marks it as `ESCALATED` and notifies the escalation target defined in the workflow step
- [ ] Given an escalated task, when the original approver subsequently acts on it, then their action is still accepted (escalation is a notification, not a reassignment)
- [ ] Given an escalation configuration per workflow step, when defined, then it includes `escalation_target_position_id` and `hours_until_escalation`
- [ ] Given the escalation job, when run, then it processes all overdue tasks in a single pass and records the escalation event in the audit log

**Priority:** Should Have  
**Complexity:** M  
**Dependencies:** Approval Task Routing, Notification Service

---

### Feature: Approval History and Audit Trail

**Description:** Full immutable audit log of all approval actions per document.

#### Story: As an auditor, I want to retrieve the complete approval history of a document so that I can reconstruct every decision

**Acceptance Criteria:**
- [ ] Given a document `type` and `id`, when `GET /approval/history?document_type=X&document_id=Y` is called, then all workflow instances, steps, and actions are returned in chronological order
- [ ] Given any approval action (approve, reject, escalate), when it occurs, then an audit record is written with `actor_id`, `action`, `timestamp`, `comment`, and `step_sequence`
- [ ] Given an audit record, when written, then it is immutable (no update or delete operations are exposed)
- [ ] Given an approval history query, when the document has been submitted multiple times (e.g., resubmitted after rejection), then all historical workflow instances are included

**Priority:** Must Have  
**Complexity:** S  
**Dependencies:** Approve / Reject feature

---

## Epic 3: Cross-Cutting — Notification Service

**Description:** A platform-wide push notification service supporting FCM (Android) and APNs (iOS) with per-user device registration, targeting by user or role, and notification history.  
**Priority:** Must Have  
**Estimated Effort:** L

---

### Feature: Device Registration

**Description:** Register and manage push notification device tokens per user.

#### Story: As a mobile app user, I want to register my device for push notifications so that I receive real-time alerts

**Acceptance Criteria:**
- [ ] Given a registration request with `user_id`, `platform` (FCM/APNs), and `token`, when submitted, then the token is stored linked to the user
- [ ] Given a user who re-registers (app reinstall), when a new token is submitted for the same `user_id` and `platform`, then the old token is replaced
- [ ] Given a stale token (returned as invalid by FCM/APNs), when detected during a send, then the token is automatically removed from the registry
- [ ] Given a deregister request with `user_id` and `token`, when submitted, then the token is removed and no further notifications are sent to it

**Priority:** Must Have  
**Complexity:** S  
**Dependencies:** Azure Entra ID or API Key authentication

---

### Feature: Push Notification Dispatch

**Description:** Send push notifications to specific users or groups.

#### Story: As a service, I want to send a push notification to a specific user so that they are alerted to an event

**Acceptance Criteria:**
- [ ] Given a send-notification request with `user_id`, `title`, `body`, and optional `data` payload, when submitted, then the notification is dispatched to all registered devices for that user
- [ ] Given a user with no registered devices, when a notification is sent, then the call succeeds (`202 Accepted`) with a note that no devices were found
- [ ] Given a send request, when processed, then it is enqueued and dispatched asynchronously (caller is not blocked on FCM/APNs response)
- [ ] Given an FCM/APNs delivery failure, when detected, then the failure is logged with the token, user, and error reason

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Device Registration

#### Story: As a service, I want to broadcast a notification to all users in a role or group so that announcements reach targeted audiences

**Acceptance Criteria:**
- [ ] Given a broadcast request with `target_role`, `title`, and `body`, when submitted, then the notification is dispatched to all devices of users holding that role
- [ ] Given a broadcast to a large group (1000+ users), when dispatched, then it is processed in batches to avoid FCM/APNs rate limits
- [ ] Given a broadcast, when completed, then a summary record is written with `sent_count`, `failed_count`, and `timestamp`

**Priority:** Should Have  
**Complexity:** M  
**Dependencies:** Push Notification Dispatch, SAP organizational data (roles)

---

### Feature: Notification History

**Description:** Store and retrieve sent notification records per user.

#### Story: As a mobile user, I want to retrieve my notification history so that I can review past alerts I may have missed

**Acceptance Criteria:**
- [ ] Given a history request for `user_id`, when called, then all notifications sent to that user are returned in descending chronological order
- [ ] Given a notification record, when returned, then it includes `id`, `title`, `body`, `data`, `sent_at`, and `delivery_status`
- [ ] Given pagination parameters `page` and `page_size`, when supplied, then the history response respects them
- [ ] Given notifications older than 90 days, when a cleanup job runs, then they are archived or deleted per retention policy

**Priority:** Should Have  
**Complexity:** S  
**Dependencies:** Push Notification Dispatch

---

### Feature: Notification Preference Management

**Description:** Allow users to opt-in or opt-out of specific notification categories.

#### Story: As a user, I want to manage my notification preferences so that I only receive alerts I care about

**Acceptance Criteria:**
- [ ] Given a preferences request for `user_id`, when called, then current preferences across all notification categories are returned
- [ ] Given a preference update with `category` and `enabled: false`, when submitted, then notifications of that category are suppressed for that user
- [ ] Given a notification send, when processed, then the recipient's preferences are checked and the notification is skipped if the category is disabled
- [ ] Given a new notification category added to the system, when it first appears, then it defaults to `enabled: true` for all users

**Priority:** Could Have  
**Complexity:** S  
**Dependencies:** Notification History

---

## Epic 4: Member Management API

**Description:** APIs for the Meka Promotion App supporting member registration via phone OTP, profile management, QR coupon code issuance, and member lookup/deactivation. BFF target: Meka Promotion App.  
**Priority:** Must Have  
**Estimated Effort:** L

---

### Feature: Member Registration (OTP)

**Description:** Register new members using phone number with OTP verification.

#### Story: As a customer, I want to register as a member using my phone number so that I can access promotions and earn benefits

**Acceptance Criteria:**
- [ ] Given a registration request with `phone_number`, when submitted, then an OTP is sent to the number via SMS and a session token is returned
- [ ] Given a valid OTP and session token, when submitted within 5 minutes, then membership registration completes and a member record is created with status `ACTIVE`
- [ ] Given an invalid OTP, when submitted, then `400 Bad Request` is returned with remaining attempts count
- [ ] Given 3 consecutive failed OTP attempts, when detected, then the session is invalidated and the user must request a new OTP
- [ ] Given an OTP that has expired (>5 minutes), when submitted, then `400 Bad Request` is returned prompting a new OTP request
- [ ] Given a phone number already registered as an active member, when a registration request is made, then `409 Conflict` is returned
- [ ] Given a successful registration, when completed, then the member receives a welcome push notification

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Notification Service (Epic 3), SMS gateway integration

---

### Feature: Member Profile Management

**Description:** View and update member profile information.

#### Story: As a member, I want to view and update my profile so that my information is accurate

**Acceptance Criteria:**
- [ ] Given an authenticated member, when `GET /members/me` is called, then their profile is returned including `member_id`, `phone_number`, `name`, `email`, `status`, `registered_at`
- [ ] Given a profile update request with `name` and/or `email`, when submitted, then the profile is updated and the updated record is returned
- [ ] Given a `phone_number` update request, when submitted, then a new OTP flow is triggered before the change takes effect
- [ ] Given a `member_id`, when `GET /members/{id}` is called by staff (with appropriate auth), then the member's profile is returned

**Priority:** Must Have  
**Complexity:** S  
**Dependencies:** Member Registration

---

### Feature: QR Code / Coupon Issuance

**Description:** Generate and manage QR code coupons tied to a member's active promotions.

#### Story: As a member, I want to view and present a QR code coupon so that I can redeem it at the POS

**Acceptance Criteria:**
- [ ] Given an authenticated member, when `GET /members/me/coupons` is called, then all active coupons are returned with their QR code data
- [ ] Given a coupon, when the QR code is generated, then it encodes a signed, short-lived token (JWT or HMAC) containing `coupon_id` and `member_id`
- [ ] Given a QR code token, when validated by the POS API, then signature and expiry are verified before redemption is allowed
- [ ] Given a coupon already redeemed, when scanned again, then the POS API returns `409 Conflict` (already used)
- [ ] Given a promotion that grants a coupon to eligible members, when the promotion becomes active, then coupons are issued to all eligible members or on-demand at first browse

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Member Registration, Promotions API (Epic 5)

---

### Feature: Member Lookup and Status Management

**Description:** Staff-facing member lookup and deactivation capabilities.

#### Story: As a staff member, I want to look up a member by phone number so that I can assist them at the counter

**Acceptance Criteria:**
- [ ] Given a lookup request with `phone_number`, when submitted by authenticated staff, then the matching member profile is returned or `404 Not Found`
- [ ] Given a lookup, when no exact match is found, then fuzzy/partial results are not returned (exact phone match only for privacy)

**Priority:** Must Have  
**Complexity:** S  
**Dependencies:** Authorization Service (staff role check)

#### Story: As an administrator, I want to deactivate or suspend a member account so that I can respond to abuse or customer requests

**Acceptance Criteria:**
- [ ] Given a deactivate request with `member_id` and `reason`, when submitted by an authorized admin, then the member's status changes to `INACTIVE`
- [ ] Given an `INACTIVE` member, when they attempt to use their QR coupon, then redemption is denied with an appropriate error
- [ ] Given a suspend request, when submitted, then the member's status changes to `SUSPENDED` and a timestamp and reason are recorded
- [ ] Given a reactivation request, when submitted by an admin, then the member's status returns to `ACTIVE`
- [ ] Given any status change, when applied, then an audit record is written with the admin's identity, reason, and timestamp

**Priority:** Must Have  
**Complexity:** S  
**Dependencies:** Authorization Service

---

## Epic 5: Promotions API

**Description:** Define promotion formulas, manage their lifecycle through approval, expose them to consumers, calculate discounts, and trigger notifications. BFF targets: Meka Promotion App + M2 Portal.  
**Priority:** Must Have  
**Estimated Effort:** XL

---

### Feature: Promotion Formula Definition

**Description:** Create and manage promotion formulas with various discount types.

#### Story: As a promotion manager, I want to define a promotion formula so that discount rules are captured for use at the POS

**Acceptance Criteria:**
- [ ] Given a create-promotion request with `name`, `description`, `discount_type`, `discount_parameters`, `eligible_products[]`, `start_date`, `end_date`, and `max_redemptions`, when submitted, then the promotion is created with status `DRAFT`
- [ ] Given `discount_type = PERCENTAGE`, when defined, then `discount_parameters` must include `percentage` (0–100)
- [ ] Given `discount_type = FIXED_AMOUNT`, when defined, then `discount_parameters` must include `amount` and `currency`
- [ ] Given `discount_type = BUY_X_GET_Y`, when defined, then `discount_parameters` must include `buy_quantity`, `get_quantity`, and `get_product_id` (or `any_product: true`)
- [ ] Given `discount_type = BUNDLE`, when defined, then `discount_parameters` must include the bundle product list and the bundle price
- [ ] Given a promotion with `end_date` before `start_date`, when submitted, then `400 Bad Request` is returned
- [ ] Given a draft promotion, when a user with `PROMOTION_WRITE` auth object permission calls update, then changes are accepted

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Authorization Service, SAP product master data (Epic 9)

---

### Feature: Promotion Lifecycle Management

**Description:** Manage the state machine: DRAFT → PENDING_APPROVAL → ACTIVE → EXPIRED / CANCELLED.

#### Story: As a promotion manager, I want promotion status to be managed through a defined lifecycle so that invalid promotions never reach customers

**Acceptance Criteria:**
- [ ] Given a promotion in `DRAFT`, when submitted for approval, then status transitions to `PENDING_APPROVAL`
- [ ] Given a promotion in `PENDING_APPROVAL`, when approved, then status transitions to `ACTIVE` if `start_date` is in the past or today, else `SCHEDULED`
- [ ] Given a promotion in `PENDING_APPROVAL`, when rejected, then status transitions to `REJECTED` and the manager is notified
- [ ] Given a promotion in `ACTIVE` or `SCHEDULED`, when `end_date` is reached, then a scheduler job transitions it to `EXPIRED`
- [ ] Given a promotion in `ACTIVE`, when cancelled by an authorized user, then status transitions to `CANCELLED` with a cancellation reason
- [ ] Given a promotion in `DRAFT`, when cancelled, then it transitions directly to `CANCELLED` without requiring approval

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Promotion Formula Definition

---

### Feature: Submit Promotion for Approval

**Description:** Integration point between Promotions API and the Approval Workflow Engine.

#### Story: As a promotion manager, I want to submit a promotion for approval so that it can be reviewed before going live

**Acceptance Criteria:**
- [ ] Given a promotion in `DRAFT`, when submit-for-approval is called, then the Approval Engine is invoked with `document_type=PROMOTION` and `document_id`
- [ ] Given the approval engine creating a workflow instance, when the first approver is assigned, then the promotion manager sees status `PENDING_APPROVAL`
- [ ] Given the approval engine approving the workflow, when the callback is received, then the promotion status is updated accordingly
- [ ] Given the approval engine rejecting the workflow, when the callback is received, then the promotion status is set to `REJECTED` and rejection comments are stored against the promotion

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Approval Workflow Engine (Epic 2), Promotion Lifecycle Management

---

### Feature: Browse Active Promotions

**Description:** Public-facing, paginated endpoint for the Meka Promotion App to browse active promotions.

#### Story: As a customer using the Meka app, I want to browse current promotions so that I can discover deals available to me

**Acceptance Criteria:**
- [ ] Given `GET /promotions?status=ACTIVE`, when called, then all currently active promotions are returned paginated
- [ ] Given pagination parameters `page` and `page_size`, when supplied, then results are paginated accordingly (default page_size: 20)
- [ ] Given a promotion, when returned, then it includes `id`, `name`, `description`, `discount_type`, `discount_summary`, `start_date`, `end_date`, and `image_url`
- [ ] Given filter parameters `category` or `product_id`, when supplied, then only matching promotions are returned
- [ ] Given this endpoint, when called without authentication, then it is accessible with only an API Key (public consumer access)
- [ ] Given 100 concurrent requests, when handled, then response time is under 300ms (promotions list is cached with a short TTL, e.g., 60 seconds)

**Priority:** Must Have  
**Complexity:** S  
**Dependencies:** Promotion Lifecycle Management

---

### Feature: Promotion Discount Calculation Engine

**Description:** A calculation service that takes a basket (product list + quantities + member context) and returns applicable discounts.

#### Story: As the POS system, I want to calculate promotion discounts for a given basket so that the correct price is presented to the customer

**Acceptance Criteria:**
- [ ] Given a calculation request with `basket_items[]` (product_id, quantity, unit_price) and optional `member_id`, when submitted, then all applicable active promotions are evaluated and the best non-conflicting discount set is returned
- [ ] Given multiple promotions applicable to the same item, when evaluated, then the engine applies the promotion with the greatest discount to the customer (most-favorable selection)
- [ ] Given a promotion with `max_redemptions` reached, when evaluated, then that promotion is excluded from calculation results
- [ ] Given a member-exclusive promotion, when `member_id` is not provided or the member is `INACTIVE`, then that promotion is excluded
- [ ] Given a calculation result, when returned, then it includes `original_total`, `discount_breakdown[]` (promotion_id, description, discount_amount), and `final_total`
- [ ] Given a calculation request, when processed, then the response time is under 200ms for baskets with up to 50 line items

**Priority:** Must Have  
**Complexity:** L  
**Dependencies:** Promotion Lifecycle Management, Member Management (Epic 4)

---

### Feature: Push Notification on New Promotion

**Description:** Trigger push notifications to relevant users when a new promotion becomes active.

#### Story: As a customer, I want to be notified when a new promotion is available so that I can take advantage of it promptly

**Acceptance Criteria:**
- [ ] Given a promotion transitioning to `ACTIVE` status, when the transition occurs, then a push notification is triggered to all registered app users (or a targeted segment)
- [ ] Given a targeted promotion (e.g., by member tier or location), when the notification is sent, then only users in the target segment are notified
- [ ] Given the notification trigger, when fired, then it calls the Notification Service broadcast API asynchronously
- [ ] Given a notification preference with promotion alerts disabled, when the notification is sent, then the user does not receive it

**Priority:** Should Have  
**Complexity:** S  
**Dependencies:** Notification Service (Epic 3), Promotion Lifecycle Management

---

## Epic 6: Sales API

**Description:** Core transactional API for the Meka POS System covering transaction creation with promotions, ECR integration, void, and return. BFF target: Meka POS System.  
**Priority:** Must Have  
**Estimated Effort:** XL

---

### Feature: Create Sales Transaction

**Description:** Create a sales transaction header and line items with promotion discounts applied.

#### Story: As a cashier, I want to create a sales transaction so that a customer purchase is recorded

**Acceptance Criteria:**
- [ ] Given a create-transaction request with `cashier_id`, `terminal_id`, `basket_items[]`, and optional `member_id`, when submitted, then a transaction is created with status `PENDING` and a unique `transaction_id`
- [ ] Given a transaction, when line items are added, then each item records `product_id`, `quantity`, `unit_price`, `discount_amount`, and `line_total`
- [ ] Given a `member_id` in the request, when processed, then the discount calculation engine is invoked automatically and discounts are applied to eligible lines
- [ ] Given a transaction, when created, then `gross_total`, `discount_total`, and `net_total` are calculated and stored
- [ ] Given a `basket_item` with a `product_id` not found in the product master, when submitted, then `400 Bad Request` is returned listing the unknown products
- [ ] Given a transaction request, when processed, then idempotency is enforced via an `Idempotency-Key` header (same key returns the same transaction without re-creation)

**Priority:** Must Have  
**Complexity:** L  
**Dependencies:** Promotion Discount Calculation Engine (Epic 5), SAP product master data (Epic 9), Authorization Service

---

### Feature: ECR Integration

**Description:** Finalize a sale by posting to the Electronic Cash Register and generating a receipt.

#### Story: As a cashier, I want to finalize a transaction via the ECR so that payment is processed and a receipt is issued

**Acceptance Criteria:**
- [ ] Given a finalize request for a `PENDING` transaction, when submitted, then the ECR integration is called with the transaction details
- [ ] Given a successful ECR response, when received, then the transaction status transitions to `COMPLETED` and `ecr_reference` is stored
- [ ] Given an ECR failure response, when received, then the transaction remains `PENDING` and the error details are returned to the caller
- [ ] Given a completed transaction, when a receipt is requested, then a receipt document is generated with `transaction_id`, `items`, `totals`, `discounts`, `payment_method`, `cashier`, `terminal`, `timestamp`, and `ecr_reference`
- [ ] Given an ECR timeout, when detected, then the integration retries up to 3 times with exponential backoff before returning an error

**Priority:** Must Have  
**Complexity:** L  
**Dependencies:** Create Sales Transaction, ECR vendor documentation/SDK

---

### Feature: Sales Void

**Description:** Reverse a completed transaction before end-of-day reconciliation.

#### Story: As a supervisor, I want to void a completed transaction so that an erroneous sale is fully reversed

**Acceptance Criteria:**
- [ ] Given a void request with `transaction_id` and `reason`, when submitted by a user with `SALES_VOID` authorization, then the transaction status transitions to `VOIDED`
- [ ] Given a void, when processed, then a reversal entry is created that offsets all line items and totals
- [ ] Given a void, when the transaction has been ECR-finalized, then the void is also posted to the ECR
- [ ] Given a transaction that is already `VOIDED` or `RETURNED`, when a void is attempted, then `409 Conflict` is returned
- [ ] Given a void, when completed, then an audit record is written with `voided_by`, `reason`, and `timestamp`
- [ ] Given a void, when the transaction includes a redeemed coupon, then the coupon is re-instated as available for the member

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Create Sales Transaction, ECR Integration, Authorization Service

---

### Feature: Sales Return

**Description:** Process partial or full returns with refund calculation.

#### Story: As a cashier, I want to process a sales return so that a customer receives a refund for returned items

**Acceptance Criteria:**
- [ ] Given a return request with `transaction_id` and `return_items[]` (product_id, quantity), when submitted, then a return transaction is created linked to the original
- [ ] Given return items, when validated, then quantities cannot exceed the originally purchased quantities minus any previous returns
- [ ] Given a return, when processed, then the refund amount is calculated based on the original line price minus proportional discounts
- [ ] Given a full return (all items), when processed, then the original transaction status transitions to `FULLY_RETURNED`
- [ ] Given a partial return, when processed, then the original transaction status transitions to `PARTIALLY_RETURNED`
- [ ] Given a return, when processed, then the ECR is notified of the refund transaction
- [ ] Given a return involving a coupon-discounted item, when processed, then the coupon is marked `USED` (not reinstated) unless the return is for the full original transaction

**Priority:** Must Have  
**Complexity:** L  
**Dependencies:** Sales Void feature, ECR Integration, Authorization Service

---

### Feature: Sales Transaction History and Reporting

**Description:** Query sales history by terminal, cashier, date range, and member.

#### Story: As a store manager, I want to query sales transactions so that I can review daily performance and investigate issues

**Acceptance Criteria:**
- [ ] Given `GET /sales/transactions` with filters `terminal_id`, `cashier_id`, `date_from`, `date_to`, `status`, when called by an authorized user, then matching transactions are returned paginated
- [ ] Given a single transaction query `GET /sales/transactions/{id}`, when called, then the full transaction is returned including all line items, discounts, and status history
- [ ] Given a summary query `GET /sales/summary?date=YYYY-MM-DD&terminal_id=X`, when called, then aggregate totals (gross, discounts, net, transaction count) are returned
- [ ] Given a reporting query over 30+ days, when submitted, then results are returned within 5 seconds via indexed queries or a pre-aggregated reporting table
- [ ] Given a member `id`, when `GET /sales/transactions?member_id=X` is called, then all transactions associated with that member are returned

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Create Sales Transaction

---

## Epic 7: Attendance API

**Description:** Staff clock-in/clock-out management with attendance records and reporting. BFF target: Meka POS System.  
**Priority:** Must Have  
**Estimated Effort:** M

---

### Feature: Staff Clock-In

**Description:** Record staff clock-in with optional device/location validation.

#### Story: As a staff member, I want to clock in using the POS app so that my attendance is recorded

**Acceptance Criteria:**
- [ ] Given a clock-in request with `staff_id` and `terminal_id`, when submitted, then an attendance record is created with `clock_in_at` = current timestamp and status `CLOCKED_IN`
- [ ] Given a staff member who is already clocked in (no clock-out recorded), when a new clock-in is attempted, then `409 Conflict` is returned
- [ ] Given a clock-in request, when submitted from a terminal not assigned to the staff member's location, then the request is logged with a `LOCATION_MISMATCH` flag (configurable: warn vs. block)
- [ ] Given a successful clock-in, when processed, then `staff_id`, `terminal_id`, `clock_in_at`, and `location_id` are recorded

**Priority:** Must Have  
**Complexity:** S  
**Dependencies:** SAP organizational data (staff, positions) from Epic 9, Authorization Service

---

### Feature: Staff Clock-Out

**Description:** Record staff clock-out and compute worked hours.

#### Story: As a staff member, I want to clock out using the POS app so that my worked hours are recorded accurately

**Acceptance Criteria:**
- [ ] Given a clock-out request with `staff_id`, when submitted, then the open attendance record is closed with `clock_out_at` = current timestamp
- [ ] Given a clock-out, when processed, then `duration_minutes` is calculated and stored
- [ ] Given a staff member who is not clocked in, when a clock-out is attempted, then `400 Bad Request` is returned
- [ ] Given a clock-out, when processed, then an event is emitted for downstream payroll or HR processing (if integration exists)

**Priority:** Must Have  
**Complexity:** S  
**Dependencies:** Staff Clock-In

---

### Feature: Attendance Records and Reporting

**Description:** View and export attendance records per staff member.

#### Story: As a store manager, I want to view attendance records for my team so that I can manage schedules and verify hours

**Acceptance Criteria:**
- [ ] Given `GET /attendance?staff_id=X&date_from=Y&date_to=Z`, when called by an authorized manager, then all matching attendance records are returned
- [ ] Given an attendance record, when returned, then it includes `staff_id`, `staff_name`, `clock_in_at`, `clock_out_at`, `duration_minutes`, `terminal_id`, `location_id`, and any flags
- [ ] Given a date range query, when the staff member has no records in that range, then an empty list is returned (not 404)
- [ ] Given `GET /attendance/summary?location_id=X&date=YYYY-MM-DD`, when called, then a summary of all staff clock-in/out events for that day and location is returned
- [ ] Given a staff member who forgot to clock out, when the end-of-day job runs, then the record is flagged as `MISSING_CLOCK_OUT` and the manager is notified

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** Staff Clock-In, Staff Clock-Out, Authorization Service

---

## Epic 8: Goods Receipt API

**Description:** API for POS staff to confirm goods received against SAP-planned deliveries, report discrepancies, and trigger SAP goods movement postings. BFF target: Meka POS System.  
**Priority:** Must Have  
**Estimated Effort:** L

---

### Feature: List Expected Deliveries

**Description:** Retrieve planned inbound deliveries from SAP for a given location.

#### Story: As a store receiver, I want to see expected deliveries so that I know what to expect and can prepare for receipt

**Acceptance Criteria:**
- [ ] Given `GET /goods-receipt/expected-deliveries?location_id=X`, when called, then all open inbound deliveries from SAP for that location are returned
- [ ] Given a delivery, when returned, then it includes `delivery_id`, `sap_document_number`, `expected_date`, `supplier`, `items[]` (material_id, material_description, expected_quantity, unit_of_measure)
- [ ] Given the SAP integration being unavailable, when the endpoint is called, then a `503` is returned with a retry hint
- [ ] Given deliveries already confirmed, when the list is fetched, then only unconfirmed deliveries are returned by default (confirmed deliveries accessible via `?include_confirmed=true`)

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** SAP Integration Layer (Epic 9)

---

### Feature: Confirm Goods Receipt

**Description:** Staff confirm received quantities against expected delivery, line by line.

#### Story: As a store receiver, I want to confirm the quantities I actually received so that inventory is updated accurately

**Acceptance Criteria:**
- [ ] Given a confirm-receipt request with `delivery_id` and `received_items[]` (material_id, received_quantity), when submitted, then a goods receipt record is created with status `PENDING_SAP_POST`
- [ ] Given received quantities, when any item has `received_quantity` different from `expected_quantity`, then the item is flagged as a discrepancy
- [ ] Given a goods receipt created, when `received_quantity == expected_quantity` for all items, then the record status is `CONFIRMED` (no discrepancies)
- [ ] Given a goods receipt created, when any discrepancy exists, then the record status is `CONFIRMED_WITH_DISCREPANCY`
- [ ] Given a confirm request for an already-confirmed delivery, when submitted, then `409 Conflict` is returned

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** List Expected Deliveries

---

### Feature: Discrepancy Reporting

**Description:** Report and manage discrepancies between expected and received quantities.

#### Story: As a store manager, I want to view and act on goods receipt discrepancies so that supplier and inventory records are kept accurate

**Acceptance Criteria:**
- [ ] Given `GET /goods-receipt/discrepancies?location_id=X&status=OPEN`, when called, then all open discrepancy records are returned
- [ ] Given a discrepancy, when returned, then it includes `delivery_id`, `material_id`, `expected_quantity`, `received_quantity`, `variance`, `reported_by`, `reported_at`
- [ ] Given a discrepancy, when acknowledged by a manager with a note, then its status transitions to `ACKNOWLEDGED`
- [ ] Given a discrepancy, when resolved (e.g., supplier credit received), then its status transitions to `RESOLVED` with resolution notes

**Priority:** Should Have  
**Complexity:** S  
**Dependencies:** Confirm Goods Receipt

---

### Feature: SAP Goods Movement Posting

**Description:** Post confirmed goods receipts to SAP as goods movements to update inventory.

#### Story: As the system, I want to automatically post confirmed receipts to SAP so that inventory is updated in real time

**Acceptance Criteria:**
- [ ] Given a goods receipt record in `PENDING_SAP_POST`, when the posting job runs, then the SAP goods movement API is called with the confirmed quantities
- [ ] Given a successful SAP posting, when confirmed, then the goods receipt record status transitions to `POSTED_TO_SAP` and the SAP material document number is stored
- [ ] Given a SAP posting failure, when detected, then the record remains `PENDING_SAP_POST` and is queued for retry (max 3 attempts with exponential backoff)
- [ ] Given 3 consecutive SAP posting failures, when reached, then the record transitions to `SAP_POST_FAILED` and an alert is raised to the operations team
- [ ] Given a `CONFIRMED_WITH_DISCREPANCY` receipt, when posted to SAP, then only the `received_quantity` values are posted (not expected quantities)

**Priority:** Must Have  
**Complexity:** L  
**Dependencies:** Confirm Goods Receipt, SAP Integration Layer (Epic 9)

---

## Epic 9: SAP Integration Layer

**Description:** The adapter layer connecting the platform to SAP for master data, organizational data, goods movements, and other SAP operations. All other epics that touch SAP data go through this layer.  
**Priority:** Must Have  
**Estimated Effort:** XL

---

### Feature: SAP Connector Setup

**Description:** Establish and configure the SAP integration connector (RFC/BAPI or REST/OData).

#### Story: As a developer, I want a configured SAP connector so that all SAP calls go through a single, managed integration point

**Acceptance Criteria:**
- [ ] Given the SAP environment credentials (`SAP_HOST`, `SAP_CLIENT`, `SAP_USERNAME`, `SAP_PASSWORD` or OAuth credentials for OData), when the connector starts, then a connection is established and verified
- [ ] Given any SAP call, when made through the connector, then it is logged with `sap_function`, `input_parameters` (sanitized), `response_status`, and `duration_ms`
- [ ] Given an SAP connection failure at startup, when detected, then the service starts in degraded mode and exposes a health check indicating SAP connectivity as unhealthy
- [ ] Given the connector, when deployed, then connection pooling is configured to manage concurrent SAP sessions
- [ ] Given the connector, when the SAP environment is configured via environment variables, then no credentials are hardcoded

**Priority:** Must Have  
**Complexity:** L  
**Dependencies:** Container Infrastructure (Epic 1)

---

### Feature: Master Data Sync — Products and Pricing

**Description:** Synchronize SAP material master and pricing data into the platform's local store for fast query access.

#### Story: As the platform, I want product and pricing data synced from SAP so that POS and promotion calculations do not need to call SAP in real time

**Acceptance Criteria:**
- [ ] Given a full sync trigger, when executed, then all active SAP materials and their prices are fetched and upserted into the local product store
- [ ] Given a delta sync schedule (e.g., every 15 minutes), when run, then only materials changed since the last sync timestamp are fetched and applied
- [ ] Given a synced product, when stored, then it includes `material_id`, `description`, `unit_of_measure`, `material_group`, `base_price`, `currency`, and `valid_from` / `valid_to` for pricing
- [ ] Given a material marked inactive in SAP, when the sync runs, then the product is marked inactive in the local store
- [ ] Given a sync run, when completed, then a sync log record is written with `started_at`, `finished_at`, `records_processed`, `records_failed`, and `sync_type`

**Priority:** Must Have  
**Complexity:** L  
**Dependencies:** SAP Connector Setup

---

### Feature: Organizational Data Sync — Staff, Positions, Cost Centers

**Description:** Sync SAP HR organizational data (employees, positions, org units) for use in auth objects and approval workflows.

#### Story: As the platform, I want staff and position data synced from SAP so that authorization and approval routing are based on current org structure

**Acceptance Criteria:**
- [ ] Given an org data sync, when run, then staff records (employee ID, name, position, cost center, active status) are upserted into the local org store
- [ ] Given a position record, when synced, then it includes `position_id`, `position_title`, `org_unit_id`, `reports_to_position_id`
- [ ] Given a staff member terminated in SAP, when the sync runs, then their local record is marked inactive and any pending approval tasks assigned to them are flagged for reassignment
- [ ] Given a delta sync schedule (e.g., every 30 minutes), when run, then only org changes since the last sync are applied
- [ ] Given a new position added in SAP, when synced, then it becomes available for workflow template configuration

**Priority:** Must Have  
**Complexity:** L  
**Dependencies:** SAP Connector Setup

---

### Feature: Goods Movement Posting

**Description:** Post goods movements (goods receipt, goods issue) to SAP on behalf of the Goods Receipt API.

#### Story: As the goods receipt service, I want to post confirmed receipts to SAP so that SAP inventory is updated

**Acceptance Criteria:**
- [ ] Given a goods movement post request with `document_type`, `material_id`, `quantity`, `unit`, `location_id`, `sap_delivery_reference`, when submitted, then the appropriate SAP BAPI or API is called
- [ ] Given a successful SAP response, when received, then the `sap_material_document_number` is returned to the caller
- [ ] Given an SAP posting failure, when detected, then the full error response from SAP is returned (mapped to a structured error format)
- [ ] Given a goods movement of type `GOODS_RECEIPT`, when posted, then the corresponding SAP movement type (e.g., `101`) is used

**Priority:** Must Have  
**Complexity:** M  
**Dependencies:** SAP Connector Setup

---

### Feature: Error Handling, Retry Queue, and Dead-Letter

**Description:** Resilient SAP integration with automatic retry and dead-letter handling for failed calls.

#### Story: As the platform, I want failed SAP calls to be automatically retried so that transient SAP outages do not cause permanent data loss

**Acceptance Criteria:**
- [ ] Given a failed SAP call (timeout or transient error), when detected, then it is placed on a retry queue with an exponential backoff schedule (1min, 5min, 30min)
- [ ] Given a SAP call that has failed 3 consecutive times, when the third failure occurs, then it is moved to the dead-letter queue and an alert is raised
- [ ] Given the dead-letter queue, when an administrator reviews it, then each entry shows `sap_function`, `input_payload`, `failure_reason`, `failure_count`, `last_attempted_at`
- [ ] Given a dead-letter entry, when manually requeued by an administrator, then it is moved back to the retry queue and retried
- [ ] Given a non-retryable SAP error (e.g., business validation failure), when detected, then it is moved directly to dead-letter without retry

**Priority:** Must Have  
**Complexity:** L  
**Dependencies:** SAP Connector Setup

---

### Feature: SAP Integration Health Monitoring

**Description:** Expose health and performance metrics for the SAP integration to operations dashboards.

#### Story: As an operator, I want visibility into SAP integration health so that I can proactively respond to degradation

**Acceptance Criteria:**
- [ ] Given the SAP integration layer, when `/integrations/sap/health` is called, then it returns SAP connectivity status, last successful call timestamp, and queue depths (retry, dead-letter)
- [ ] Given retry queue depth exceeding 50 items, when detected, then an alert threshold is triggerable
- [ ] Given dead-letter queue having any items, when detected, then an alert is triggered immediately
- [ ] Given SAP call latency exceeding 10 seconds for 3 consecutive calls, when detected, then the integration is marked as `DEGRADED` in the health check
- [ ] Given the metrics endpoint, when polled, then SAP call success rate, average latency, and error rate by function are exposed in Prometheus format

**Priority:** Should Have  
**Complexity:** M  
**Dependencies:** SAP Connector Setup, Logging and Observability (Epic 1)

---

## Cross-Cutting Concerns and Open Questions

### Identified Cross-Epic Dependencies

| Consumer | Depends On |
|---|---|
| All Epics | Epic 1: Platform Foundation (auth, logging, error handling) |
| Epic 2 (Approval) | Epic 9 (SAP Org Data for positions) |
| Epic 3 (Notification) | Epic 1 (auth, containers) |
| Epic 4 (Members) | Epic 3 (notifications), SMS gateway |
| Epic 5 (Promotions) | Epic 2 (approval), Epic 3 (notifications), Epic 9 (product master) |
| Epic 6 (Sales) | Epic 5 (discount engine), Epic 9 (product master), ECR vendor |
| Epic 7 (Attendance) | Epic 9 (org data: staff, positions) |
| Epic 8 (Goods Receipt) | Epic 9 (SAP connector, goods movement posting) |

### Open Questions

1. **ECR Vendor:** Which ECR vendor/SDK is in scope? Protocol and API contract needed before Epic 6 sprint planning.
2. **SMS Gateway:** Which SMS provider is used for OTP (Twilio, AWS SNS, local telco)? Affects Epic 4.
3. **SAP Version & Connectivity:** RFC/BAPI or OData/REST? SAP landscape (DEV/QAS/PRD) access for the integration team?
4. **SAP Authorization Object Design:** Are the SAP-style auth object definitions pre-defined by business, or does the backend team design them from scratch?
5. **Multi-Store:** Does the platform need to support multiple store locations from day one, or is this a single-store MVP?
6. **Coupon Issuance Trigger:** Are coupons pre-issued to all eligible members on promotion activation, or generated on-demand at first browse? (Affects load profile significantly.)
7. **Approval Escalation Target:** Is the escalation target always the position's manager (SAP hierarchy), or is it configurable per workflow step?
8. **Data Residency:** Any data sovereignty requirements that affect where member PII or transaction data is stored?
9. **Offline POS Support:** Does the Meka POS System need to function offline and sync later, or is network connectivity guaranteed at the terminal?
10. **Return Refund Method:** Is refund method always back to original payment, or does the POS support store credit?
