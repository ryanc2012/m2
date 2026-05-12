Here is the improved version:

---

## Context

I am a development team lead at an enterprise organization. We are embarking on a greenfield system (platform) to drive digitalization and streamline company operations. This platform will serve as an additional layer on top of our existing SAP system, delivering enriched capabilities that SAP alone cannot provide.

Each application built on this platform will have access to the following cross-cutting components (optional, but ready to use out of the box):

- **Authorization** — SAP-style authorization objects
- **Approval** — Sequential, position-based approval workflows
- **Notification** — Real-time push notifications to front-end clients

---

## API Requirements

- Must follow the **BFF (Backend for Frontend)** pattern
- All API endpoints must be secured by:
  - **Azure Entra ID** authentication — for internal users
  - **API Keys** — for system-to-system or public application consumers
- **Container support** is mandatory
- Architecture style (microservices / monolithic / hybrid) is **yet to be decided**

---

## Project 1 — POS System

### Membership App — *"Meka Promotion App"*
**Platform:** Flutter (iOS & Android)

**Features:**
- Browse active promotions
- Receive promotional messages via push notifications
- Register as a member using a mobile phone number
- Display a QR code as a redeemable coupon

---

### Sales Front-End — *"Meka POS System"*
**Platform:** Flutter (iOS & Android)

**Features:**
- Staff clock-in and clock-out
- Create sales transactions (with promotion discount calculation and ECR integration), including sales void and sales return
- Confirm goods receipt for replenishment deliveries from the warehouse

---

### Back-Office Front-End — *"M2 Portal"*
**Platform:** ASP.NET Blazor Web App with Material Design UI

**Features:**
- Define and manage promotion formulas (requires approval workflow)

---

## Immediate Next Steps

1. **Define coding standards**
2. **Draft the architectural design**
3. **Propose the product backlog**
