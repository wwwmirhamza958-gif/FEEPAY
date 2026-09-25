# Requests to Agent A (Platform)

Owned by **S**. A implements; A does not edit this file to “close” a request — S marks status.

| ID | Status | Request | CONTRACT / design ref |
|---|---|---|---|
| R-A-001 | Open | When scaffolding `packages/money`, public API must match CONTRACT §6.1 (decimal strings, half-up per line, `formatPkr` with thin space, Western digits for EN and UR). Reject `number`-based money constructors. | CONTRACT §1, §6.1 |
| R-A-002 | Open | `packages/validation` must export `parseApiError` that always yields a usable message from CONTRACT §3 (including unknown bodies → generic fallback). Never return empty/silent failure. | CONTRACT §3, §6.2 |
| R-A-003 | Open | `packages/api-client` must be **generated** from OpenAPI; include `ApiError` / `ApiErrorDetail` schemas exactly as CONTRACT §3. No hand-written DTO forks. | CONTRACT §6.3, §9 |
| R-A-004 | Open | `packages/ui` token **names** must match CONTRACT §6.4; **values** from `DESIGN_SUPERVISOR.md` §3. Export `StatusPill` that accepts only CONTRACT status / derived display statuses — no free-text status labels. | CONTRACT §6.4 · DESIGN §3, §5.3 |
| R-A-005 | Open | Apply pilot seed **only** from CONTRACT §5 (tenant UUID, fee head codes, adapter codes, fake phones). Secret values stay in secret store; seed stores `secret_ref` only. | CONTRACT §5 |
| R-A-006 | Open | API error middleware: every non-2xx JSON body uses the §3 envelope (`error.code`, `message`, `messageKey`, `correlationId`, `details[]`, `retryable`). | CONTRACT §3 |
| R-A-007 | Open | Outbox + domain event **names** must match CONTRACT §4 when wiring infrastructure (even if few publishers exist in P0). | CONTRACT §4 |
| R-A-008 | Open | Enforce forbidden tooling in templates/CI where practical: no AutoMapper, MediatR, EPPlus, `Database.Migrate()` at startup, float money. | CONTRACT §0, §8.3 |

No open requests require A to change `DESIGN_SUPERVISOR.md`. Token values remain as specified there unless S files a design dispute note later.
