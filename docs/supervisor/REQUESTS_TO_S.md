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
