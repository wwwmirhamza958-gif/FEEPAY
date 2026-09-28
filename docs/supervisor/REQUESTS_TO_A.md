# Requests to Agent A (Platform)

Owned by **S**. A implements; A does not edit this file to “close” a request — S marks status.

| ID | Status | Request | CONTRACT / design ref |
|---|---|---|---|
| R-A-001 | **Done** (P0) | `packages/money` public API matches CONTRACT §6.1 | CONTRACT §1, §6.1 |
| R-A-002 | **Done** (P0) | `parseApiError` never silent | CONTRACT §3, §6.2 |
| R-A-003 | **Done** (P0) | Generated OpenAPI client at `packages/api-client/src/generated/` + committed `openapi.json`; CI verify | CONTRACT §6.3, §9.1 |
| R-A-004 | **Done** (P0) | UI tokens + StatusPill closed set | CONTRACT §6.4 · DESIGN §3, §5.3 |
| R-A-005 | **Done** (P0) | Pilot seed constants from §5 | CONTRACT §5 |
| R-A-006 | **Done** (P0) | API §3 error envelope | CONTRACT §3 |
| R-A-007 | **Done** (P0) | Outbox event names match §4 (completed via R-A-009) | CONTRACT §4 |
| R-A-008 | **Done** (P0) | Forbidden tooling CI checks | CONTRACT §0, §8.3 |
| R-A-009 | **Done** (P0) | Missing DomainEvents constants added | CONTRACT §4 |
| R-A-010 | **Done** (P0 partial vs plan) | Cross-tenant **EF global query filter** isolation tests in CI (InMemory). PostgreSQL **RLS** proof still deferred until Postgres available. | Plan §13 / §17 |
| R-A-011 | **Partial** | OTP + portal password login work in integration tests. Staging Postgres/Redis/SMS/TOTP still required for plan §17 “login on staging”. | Plan §17 P0 gate |
| R-A-012 | **Open** | When Docker/Postgres available: add RLS isolation test + Hangfire PG storage smoke + staging OTP/portal login checklist | Plan §5.3, §17 |
| R-A-013 | **Done** | `ImportStatusPill` covers CONTRACT §2.4 without colliding with PaymentAttempt Succeeded/Failed. | CONTRACT §2.4 · E→S-1 |
| R-A-014 | **Done** | Invoice `Draft` is on `StatusPillKind`. | CONTRACT §2.2 · E→S-2 |
| R-A-015 | **Done** | Shared `ConfirmDialog` in `packages/ui` (optional required reason). Portal still uses its local copy until E switches. | DESIGN §9 · E→S-4 |
| R-A-016 | **Open** | Replace portal `X-Tenant-Id` stub with session→tenant resolution for portal auth (P1 accepted header stub per C→S-2). | C→S-2 · Plan §5.3 |
| R-A-017 | **Done** | `DevSeed.EnsureAppliedAsync` creates the pilot tenant and calls `AcademicPilotSeed` (Development + InMemory only). | C→S-3 · CONTRACT §5 |

### A's optional asks to S (`REQUESTS_TO_S.md`)

| ID | Status | Decision |
|---|---|---|
| A→S-1 | **Done** | Frozen in CONTRACT §9.1 |
| A→S-2 | **Done** | Frozen in CONTRACT §9.1 |

No open requests require A to change `DESIGN_SUPERVISOR.md`.
