# Requests to Agent C — Billing & data

From: **Agent E — Institution portal** (P1)
Date: 2026-09-25
Branch: `agent/e-p1-portal`

Portal ships against **local typed mocks** until these OpenAPI paths exist in `packages/api-client`. Do not invent Billing EF tables in the portal.

| ID | Method | Path (proposed) | Purpose | Portal screen |
|---|---|---|---|---|
| R-C-E-001 | GET/PATCH | `/api/v1/tenants/{tenantId}/branding` | Display name + identity colour token (not button fill) | Setup → Branding |
| R-C-E-002 | GET/POST | `/api/v1/academic/programs` | Programs + intake labels | Setup → Programs |
| R-C-E-003 | GET/POST | `/api/v1/billing/fee-heads` | Fee heads; `taxable_for_236I` unused by engine | Setup → Fee heads |
| R-C-E-004 | GET | `/api/v1/billing/fee-schedules` | Version list (`version`, `effectiveFrom`, draft/published) | Setup → Fee schedules |
| R-C-E-005 | GET/POST | `/api/v1/billing/discount-categories` | Categories + stacking policy note | Setup → Discounts |
| R-C-E-006 | GET/POST | `/api/v1/billing/instalment-plans` | Instalment plan list | Setup → Instalment plans |
| R-C-E-007 | POST | `/api/v1/imports` | Upload file → job id; status per CONTRACT §2.4 | Data → Import |
| R-C-E-008 | PUT | `/api/v1/imports/{jobId}/mapping` | Column → field map | Data → Import |
| R-C-E-009 | POST | `/api/v1/imports/{jobId}/validate` | Dry run; body returns create/update/skip/error counts + error report URL | Data → Import |
| R-C-E-010 | POST | `/api/v1/imports/{jobId}/commit` | Commit after dry run; audit reason in body | Data → Import |
| R-C-E-011 | GET | `/api/v1/academic/students?q=` | Student search | Data → Students |
| R-C-E-012 | POST | `/api/v1/billing/preview-runs` | Preview for term; lines with amounts (decimal strings) + outlier flags/reasons | Billing → Preview |
| R-C-E-013 | POST | `/api/v1/billing/statements/issue` | Issue from preview id; requires `reason`; returns Issued counts | Billing → Preview |
| R-C-E-014 | POST | `/api/v1/billing/adjustments` | Adjustment/credit note; requires `reason`; supersedes per §2.2 | Billing → Adjustments |

**Error envelope:** all failures must use CONTRACT §3 (`error.message` + `retryable`). Portal already renders via `@fee/ui` `ErrorState` + `parseApiError`.

**Money:** wire amounts as `"142500.00"` strings (§1.4). Portal formats with `@fee/money.formatPkr`.
