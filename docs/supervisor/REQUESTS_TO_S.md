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
