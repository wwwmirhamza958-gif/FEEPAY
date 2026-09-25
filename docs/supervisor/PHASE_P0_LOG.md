# Phase P0 — Supervisor gate log

| | |
|---|---|
| **Phase** | P0 Foundation |
| **Branch (S)** | `agent/s-p0-contract` |
| **Contract** | `CONTRACT.md` v1 |
| **Gate reference** | `FEE_PLATFORM_IMPLEMENTATION_PLAN.md` §16.4 |
| **Status** | **Pending** — fill when Agent A finishes P0 deliverables; sign-off empty until then |

---

## P0 context (what this phase builds)

Monorepo, CI, environments, tenancy + RLS, auth (OTP + portal login/2FA), RBAC, audit, outbox, OpenAPI contract pipeline, design system shell, shared TS packages (`api-client`, `ui`, `money`, `validation`), and `CONTRACT.md` v1 (this supervisor deliverable).

**P0 gate (plan §17):** Cross-tenant isolation tests green; a portal user and a parent can log in on staging; `CONTRACT.md` ratified; supervisor gate (§16.4) complete.

---

## §16.4 Supervisor gate checklist

Complete **in order**. An abandoned or partial pass means the gate has **not** been passed.

### 1. Ownership audit

Every changed file belongs to the agent that changed it.

| Check | Result | Notes |
|---|---|---|
| No agent edited another agent's owned paths | ☐ Pending | |
| S touched only CONTRACT / supervisor / shared seed defs | ☐ Pending | |
| A touched only Platform-owned paths | ☐ Pending | |
| No feature modules from B–G merged as P0 scope creep | ☐ Pending | |

### 2. Contract conformance

Status values, error shapes, event names and money handling match `CONTRACT.md` across every module that touches them.

| Check | Result | Notes |
|---|---|---|
| Money: `numeric(18,2)` / `decimal`; no float/double | ☐ Pending | |
| Money wire: decimal strings; format via `packages/money` | ☐ Pending | |
| Status enums match CONTRACT §2 (PaymentAttempt, Invoice, Clearance, Import, Deferment) | ☐ Pending | |
| Overdue not stored as Invoice.status | ☐ Pending | |
| API errors use CONTRACT §3 envelope; clients parse it | ☐ Pending | |
| Domain / outbox event names match CONTRACT §4 | ☐ Pending | |
| OpenAPI schemas align with CONTRACT §9 | ☐ Pending | |
| Forbidden tooling absent (AutoMapper, MediatR, EPPlus, Migrate-at-startup, hand-written clients) | ☐ Pending | |

### 3. Seed and reference reconciliation

No two agents have written conflicting reference data. Check against CONTRACT §5 (and later pilot source documents), not only against each other.

| Check | Result | Notes |
|---|---|---|
| Pilot `tenant_id` / slug / fee head codes / adapter codes match §5 | ☐ Pending | |
| No real CNICs or real parent phones in seeds | ☐ Pending | |
| No duplicate contradictory bank or program codes | ☐ Pending | |

### 4. Cross-module trace

One real journey followed end to end through every module it crosses.

| Check | Result | Notes |
|---|---|---|
| **P0 minimum:** portal user login + parent OTP login on staging succeed with tenant isolation | ☐ Pending | Full payment trace is P3 (§16.4.4 example) |
| Outbox infrastructure present and tenant-scoped jobs fail closed without tenant context | ☐ Pending | |

### 5. Deferred-scope check

Nothing from plan §3.2 has been built; tax has not crept in (§7.4). CONTRACT §8 is authoritative.

| Check | Result | Notes |
|---|---|---|
| No wallet / held balances | ☐ Pending | |
| No card PAN/CVV fields or custom card form | ☐ Pending | |
| No WebView checkout | ☐ Pending | |
| No tax rules UI, tax lines, tax statements, tax events | ☐ Pending | |
| No native app work before web payment proven | ☐ Pending | |
| No BNPL / AI / LMS / school-only MVP UI | ☐ Pending | |

### 6. Written sign-off

| Field | Value |
|---|---|
| Sign-off status | **PENDING** |
| Signed by (Agent S) | — |
| Date (UTC) | — |
| What was checked | — |
| Findings / follow-ups | — |
| CONTRACT ratified (yes/no) | — |

> Do not mark the phase done until this section is filled and every box above is complete. Partial pass = gate failed.

---

## Related artefacts

- `CONTRACT.md` (repo root)
- `docs/supervisor/REQUESTS_TO_A.md`
- `DESIGN_SUPERVISOR.md` (read/request only for S unless settling a dispute)
