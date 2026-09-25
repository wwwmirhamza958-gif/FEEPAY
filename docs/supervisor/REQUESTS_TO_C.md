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

---

## From Agent B — Fee engine (P1)

Date: 2026-09-25
Branch: `agent/b-p1-fee-engine`

### Public API to call

```csharp
using FeePlatform.Modules.FeeEngine.Api;

IFeeEngine engine = new FeeEngine();
FeeEngineResult draft = engine.Calculate(input); // pure; IsPreview default true
```

Details: `backend/src/Modules/FeeEngine/README.md`

### What Billing owns (please do not push into FeeEngine)

| Concern | Note |
|---|---|
| Persist draft / issue invoice | Engine returns lines + traces only |
| Link adjustments / credit notes to original invoice | Engine accepts `AdjustmentInput[]` for **new draft math**; immutability of issued invoices is Billing |
| Daily late-fee job | Call `LateFeeCalculator` / pass `LateFeeRequest` on input; engine does not schedule |
| Instalment due-date calendar | Engine splits amounts; Billing stores parts + payment state |
| Tax persistence | Keep `TaxRule` table empty; do not invent tax lines from engine output (`TaxLines` always empty in P1) |

### Shapes B recommends C store

1. **Statement / invoice line**: `fee_head_code`, `description`, `amount numeric(18,2)`, `trace text`, `kind` ∈ {Charge, Discount, Adjustment, LateFee}, optional `rule_id` / `discount_id`
2. **Rule parameters**: JSONB + `schema_version` + `rule_type` — deserialize via `RuleParametersParser` or map to typed records under `Models.Rules`
3. **Discount stacking policy** per tenant: category order, allow-stacking, max amount and/or max % of eligible
4. **Previous-term snapshot** for diff: per-head net amounts (engine `StatementSnapshot`)

Engine P1 DoD is met on `agent/b-p1-fee-engine`. Wire Billing → `IFeeEngine` when ready.
