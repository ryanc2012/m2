# Architecture Decision Records (ADRs)

> **Status:** Approved
> **Owner:** Keyser (Lead / Architect)
> **Last Reviewed:** 2026-05-15
> **Change Log:**
> - 2026-05-15 — Initial ADR index created (Keyser)

---

## What Is an ADR?

An Architecture Decision Record captures a significant technical decision: the context that forced a choice, the decision made, its consequences, and the alternatives considered. ADRs are *immutable after approval* — they become part of the project's permanent record. A new ADR supersedes an old one; the old one is marked Deprecated and linked to its successor.

**ADRs are not design docs.** They are decision records. Keep them short. If you can't explain the decision in one page, you haven't understood it well enough yet.

---

## When to Write an ADR

Write an ADR when a decision:

- Affects **two or more** modules, services, or apps
- Is **hard to reverse** (schema design, auth approach, external protocol)
- Represents a **deliberate trade-off** where alternatives were real options
- Would cause a **"why did we do it this way?"** question in three months

**Do NOT write an ADR for:**
- Coding style choices (those go in `docs/standards/CODING-STANDARDS.md`)
- Tactical implementation decisions that affect only one file or class
- Decisions already captured in a vendor doc you're just following

When in doubt: if McManus, Fenster, or Edie need to know about it to do their jobs correctly, write the ADR.

---

## ADR Lifecycle

```
Draft → Review → Approved → [Deprecated]
```

1. **Draft** — Author creates `ADR-NNN-slug.md` from `template.md` and sets Status: Draft
2. **Review** — Author opens a PR; at least one other team member must approve the PR
3. **Approved** — Merged; now immutable. Record it in the index below
4. **Deprecated** — Only via a new ADR that supersedes it. Update the old ADR's Status field and add a `Superseded By` link

---

## Numbering

ADRs are numbered sequentially: `ADR-001`, `ADR-002`, etc.

The next available number is: **ADR-023**

> Note: ADR-001 through ADR-022 were established during the initial architecture and planning sessions (2026-05-12 to 2026-05-13) and are recorded in `.squad/decisions.md`. They will be migrated to individual files in this folder as bandwidth allows.

---

## ADR Index

| # | Title | Status | Date | Owner |
|---|-------|--------|------|-------|
| [ADR-001](../..) | Architecture Style: Modular Monolith | Approved | 2026-05-12 | Keyser |
| [ADR-002](../..) | Three Dedicated BFFs (POS, Promos, Portal) | Approved | 2026-05-12 | Keyser |
| [ADR-003](../..) | PostgreSQL 16 as Primary Database | Approved | 2026-05-12 | Keyser |
| [ADR-004](../..) | In-Process Authorization Module (SAP Auth Object Model) | Approved | 2026-05-12 | Keyser |
| [ADR-005](../..) | SignalR for Blazor Real-Time Notifications | Approved | 2026-05-12 | Keyser |
| [ADR-006](../..) | SAP Integration via OData REST (RFC/BAPI Fallback) | Approved | 2026-05-12 | Keyser |
| [ADR-007](../..) | Flutter State Management: Riverpod | Approved | 2026-05-12 | Fenster |
| [ADR-008](../..) | Blazor Code-Behind Pattern (No Inline @code Blocks) | Approved | 2026-05-12 | Fenster |
| [ADR-009–022](../..) | Planning-phase decisions (see `.squad/decisions.md`) | Approved | 2026-05-12 | Keyser |

> ADR-001 through ADR-022 are stored in `.squad/decisions.md` pending migration to this folder.

---

## How to Use the Template

1. Copy `template.md` to a new file: `ADR-NNN-brief-slug.md`
   - Use kebab-case for the slug: `ADR-023-api-versioning-strategy.md`
2. Fill in every section. Do not leave placeholders.
3. Set Status to **Draft** and open a PR.
4. After approval, add a row to the index table above.

---

## Related

- [ADR Template → `docs/adr/template.md`](./template.md)
- [Documentation Standards → `docs/standards/README.md`](../standards/README.md)
- [Historic decisions → `.squad/decisions.md`](../../.squad/decisions.md)
