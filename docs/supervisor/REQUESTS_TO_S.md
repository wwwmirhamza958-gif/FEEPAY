# Requests to Supervisor (Agent S)

From: **Agent A — Platform** (P0)
Date: 2026-09-25
Status: Aligned to `CONTRACT.md` v1 after it landed on the branch.

## Closed by alignment (no CONTRACT edit needed)

- §1 / §6.1 money package (decimal strings, half-up, formatPkr)
- §3 error envelope in API stubs + `packages/validation.parseApiError`
- §2 status enums + StatusPill kinds
- §4 domain event name constants on outbox
- §5 pilot seed constants (`PilotSeed`) with `secret_ref` pattern only
- §6.4 token names in `packages/ui`

## Open asks (optional / later)

| ID | Ask |
|---|---|
| A→S-1 | Confirm OpenAPI generation artifact path for `@fee/api-client` (CI commit vs download). |
| A→S-2 | When NSwag vs `Microsoft.AspNetCore.OpenApi` + `openapi-typescript` is frozen, note it in CONTRACT §9. |

No requests to change `DESIGN_SUPERVISOR.md` token values.

---

## Agent A — Platform (P1 follow-up)

Date: 2026-09-28
Branch: `agent/a-p1-ui`

### Closed this pass (implementation; S marks REQUESTS_TO_A)

- R-A-013 — `ImportStatusPill` for CONTRACT §2.4 (no collide with PaymentAttempt Succeeded/Failed)
- R-A-014 — Invoice `Draft` on `StatusPillKind`
- R-A-015 — `ConfirmDialog` in `packages/ui` (optional required reason, portal density)
- R-A-017 — `DevSeed` calls `AcademicPilotSeed.EnsureAppliedAsync` (Development + InMemory only)

### Stays open — needs S / infra

| ID | Why open |
|---|---|
| R-A-012 | No Docker/Postgres in this worktree; RLS + Hangfire PG smoke deferred. |
| R-A-016 | Replacing portal `X-Tenant-Id` stub with session→tenant needs real portal auth binding (cookie/bearer session lookup) and safe rollout against staging Postgres — not a safe half-step on InMemory-only. Header stub remains per C→S-2 until auth hardening. |

---

## Agent B — Fee engine (P1)

Date: 2026-09-25
Branch: `agent/b-p1-fee-engine`

### Implemented without CONTRACT edit

- Money half-up / wire / `FormatPkr` mirrored in `FeePlatform.Modules.FeeEngine.Money.MoneyMath` (same semantics as `packages/money`)
- Canonical fee-head codes from CONTRACT §5.5 used in fixtures
- Tax slot accepted on input; **no tax lines emitted** (§7.4 / §8.2)

### Open asks

| ID | Ask | Priority |
|---|---|---|
| B→S-1 | Add a canonical fee-head code for late-fee adjustment lines (engine default `LATE_FEE` today). Prefer seed row in §5.5 or an explicit “adjustment head codes” note so C/E do not invent synonyms. | Medium |
| B→S-2 | Optional: freeze JSON schema_version = 1 parameter property names for each `FeeRuleType` in CONTRACT or an annex, so Billing persistence of rule JSONB matches the engine parser. | Low (engine owns typed records + `RuleParametersParser` for now) |

---

## From Agent C — Billing & data (P1)

| ID | Ask |
|---|---|
| C→S-1 | Confirm enrolment lifecycle words `Enrolled` / `Dropped` are acceptable (not in §2 vocabularies). |
| C→S-2 | Confirm P1 portal tenant header `X-Tenant-Id` stub until A wires session→tenant. |
| C→S-3 | `AcademicPilotSeed` (C) should be called from A's official seed runner for §5. |
| C→S-4 | Fee-schedule / discount write APIs return 501 until B owns mutable config — confirm OK for P1 Design Gate. |
| C→S-5 | Note: Billing wires `IFeeEngine` from B; fee-head catalog includes `LATE_FEE` per CONTRACT v1.2. |
