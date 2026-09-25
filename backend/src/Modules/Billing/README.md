# Agent C — Billing & data (P1)

Owns Academic, Billing, and Integrations. Portal UI is Agent E; FeeEngine internals are Agent B.

Routes aligned with `docs/supervisor/REQUESTS_TO_C.md` (R-C-E-001…014).

## FeeEngine

Billing calls `FeePlatform.Modules.FeeEngine.Api.IFeeEngine` (DI: `FeeEngine`). Preview/issue build `FeeEngineInput` from enrolments + pilot heads/rules (`PilotFeeHeads` + `LATE_FEE`). Tax lines are never persisted.

## Route list

Tenant header: `X-Tenant-Id: <uuid>`.

| Method | Path | R-C-E |
|---|---|---|
| GET/PATCH | `/api/v1/tenants/{id}/branding` | 001 |
| GET/POST | `/api/v1/academic/programs` | 002 |
| GET | `/api/v1/academic/students?q=` | 011 |
| GET | `/api/v1/billing/fee-heads` | 003 (§5.5 + `LATE_FEE`) |
| GET | `/api/v1/billing/fee-schedules` | 004 (empty stub) |
| GET/POST | `/api/v1/billing/discount-categories` | 005 |
| GET/POST | `/api/v1/billing/instalment-plans` | 006 |
| POST | `/api/v1/billing/preview-runs` | 012 |
| POST | `/api/v1/billing/statements/issue` | 013 (`reason` required) |
| POST | `/api/v1/billing/adjustments` | 014 |
| POST | `/api/v1/imports` | 007 |
| PUT | `/api/v1/imports/{jobId}/mapping` | 008 |
| POST | `/api/v1/imports/{jobId}/validate` | 009 |
| POST | `/api/v1/imports/{jobId}/commit` | 010 |

Statuses: CONTRACT §2. Errors: §3. Money: decimal strings §1.4.

## Seed / migration

- `AcademicPilotSeed` — CONTRACT §5.3–§5.6
- `backend/migrations/002_p1_academic_billing_integrations.sql`
