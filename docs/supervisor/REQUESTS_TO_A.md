# Requests to Agent A (Platform)

Owned by **S**. A implements; A does not edit this file to “close” a request — S marks status.

| ID | Status | Request | CONTRACT / design ref |
|---|---|---|---|
| R-A-001 | **Done** (P0) | When scaffolding `packages/money`, public API must match CONTRACT §6.1 (decimal strings, half-up per line, `formatPkr` with thin space, Western digits for EN and UR). Reject `number`-based money constructors. | CONTRACT §1, §6.1 |
| R-A-002 | **Done** (P0) | `packages/validation` must export `parseApiError` that always yields a usable message from CONTRACT §3 (including unknown bodies → generic fallback). Never return empty/silent failure. | CONTRACT §3, §6.2 |
| R-A-003 | **Open** | `packages/api-client` must be **generated** from OpenAPI; include `ApiError` / `ApiErrorDetail` schemas exactly as CONTRACT §3. No hand-written DTO forks. *P0: placeholder re-exports validation types only — generation still TODO.* | CONTRACT §6.3, §9 |
| R-A-004 | **Done** (P0) | `packages/ui` token **names** must match CONTRACT §6.4; **values** from `DESIGN_SUPERVISOR.md` §3. Export `StatusPill` that accepts only CONTRACT status / derived display statuses — no free-text status labels. *Follow-up: add Deferment kinds when portal/parent request flows land.* | CONTRACT §6.4 · DESIGN §3, §5.3 |
| R-A-005 | **Done** (P0) | Apply pilot seed **only** from CONTRACT §5 (tenant UUID, fee head codes, adapter codes, fake phones). Secret values stay in secret store; seed stores `secret_ref` only. *Constants present; full seed runner still stub.* | CONTRACT §5 |
| R-A-006 | **Done** (P0) | API error middleware: every non-2xx JSON body uses the §3 envelope (`error.code`, `message`, `messageKey`, `correlationId`, `details[]`, `retryable`). *Auth stubs use `ApiErrors`; keep as the only path for new endpoints.* | CONTRACT §3 |
| R-A-007 | **Partial** | Outbox + domain event **names** must match CONTRACT §4. *Present: InvoiceIssued/Superseded, PaymentAttemptCreated, PaymentSucceeded/Failed, ClearanceChanged, ImportJobCompleted, OfflinePaymentRecorded, OverpaymentCredited. Missing constants: `PaymentReversed`, `DefermentRequested`, `DefermentApproved`, `DefermentRejected`.* | CONTRACT §4 |
| R-A-008 | **Done** (P0) | Enforce forbidden tooling in templates/CI where practical: no AutoMapper, MediatR, EPPlus, `Database.Migrate()` at startup, float money. | CONTRACT §0, §8.3 |
| R-A-009 | **Open** | Add missing `DomainEvents` constants listed under R-A-007 Partial; keep names byte-identical to CONTRACT §4. | CONTRACT §4 |
| R-A-010 | **Open** | Cross-tenant isolation automated tests in CI (plan §13 / P0 gate). Not present yet. | Plan §17 P0 gate |
| R-A-011 | **Open** | Staging: portal user login + parent OTP login succeed end-to-end (auth currently stubbed / 501 portal). Blocks full P0 gate. | Plan §17 P0 gate |

No open requests require A to change `DESIGN_SUPERVISOR.md`. Token values remain as specified there unless S files a design dispute note later.

### A's optional asks to S (`REQUESTS_TO_S.md`)

| ID | Status | Decision |
|---|---|---|
| A→S-1 | **Done** | Frozen in CONTRACT §9.1 — `packages/api-client/src/generated/`, committed on OpenAPI-change PRs. |
| A→S-2 | **Done** | Frozen in CONTRACT §9.1 — AspNetCore OpenAPI + `openapi-typescript`; NSwag optional later. |
