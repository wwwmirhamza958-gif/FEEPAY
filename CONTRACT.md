# Fee Platform — Shared Contract

| | |
|---|---|
| **Owned by** | Agent **S** (Supervisor) only |
| **Version** | **v1** — P0 Foundation |
| **Companion** | `FEE_PLATFORM_IMPLEMENTATION_PLAN.md` v0.2 · `DESIGN_SUPERVISOR.md` v1.0 |
| **Rule** | Any agent that edits this file has its work **rejected for that phase**. Request a change; do not patch locally. |

This file is the single source of truth for everything more than one agent depends on (§16.3). When two agents disagree, this document wins. Bugbot will not catch contradictory beliefs — only this contract plus the supervisor gate will.

---

## 0. How to use this document

| Audience | Must read before coding |
|---|---|
| **A** (Platform) | §1 Money · §2 Status · §3 Errors · §4 Events · §5 Seed · §6 Packages · §8 Forbidden |
| **B** (Fee engine) | §1 Money · §8 Forbidden (no tax output) |
| **C** (Billing & data) | §1 · §2 (Invoice, Deferment, Import) · §3 · §4 · §5 · §8 |
| **D** (Payments) | §1 · §2 (PaymentAttempt) · §3 · §4 · §8 |
| **E / F** (Portal / Parent web) | §2 · §3 · §7 Labels · §8 · design tokens via §6.4 |
| **G** (Native, later) | Same as F; consume packages, invent nothing |

**Stack assumptions for P0 scaffolding (treat as frozen until §18 is reopened by humans):**

| Layer | Choice |
|---|---|
| Backend | .NET 10 + EF Core + PostgreSQL + Hangfire + Redis |
| Frontends | Next.js + TypeScript + Tailwind + shadcn; pnpm + Turborepo |
| Money storage | `numeric(18,2)` / C# `decimal`; PKR only |
| **Forbidden tooling** | `float`/`double` for money · AutoMapper · MediatR · EPPlus · `Database.Migrate()` at startup · hand-written API client shapes |

---

## 1. Money rules

### 1.1 Storage and types

| Rule | Value |
|---|---|
| SQL type | `numeric(18,2)` |
| C# type | `decimal` (never `float`, `double`, `Money` wrappers that use floating point) |
| TypeScript | `string` on the wire for amounts (see §1.4); parse/format only via `packages/money` |
| Currency | **PKR only** in MVP. No multi-currency fields, no FX. |
| Precision | Exactly two decimal places in storage. Display for parents is whole rupees (see §1.3). |

### 1.2 Rounding

- Round **per invoice line** (and per instalment part when splitting), not on the grand total after summing.
- Mode: **half-up** (0.5 away from zero toward +∞ for positive amounts; document as bankers-avoiding half-up).
- Engine and Billing must use the same helper; do not reimplement rounding in UI.
- Discounts and adjustments are also rounded per line before they hit the ledger.

### 1.3 Display format (parent-facing and portal)

Implemented **once** in `packages/money`. Never hand-format in feature code.

| Case | Format | Notes |
|---|---|---|
| Positive | `Rs 142,500` | Thin space (U+2009) after `Rs`. Thousands separators with comma. Western digits in **both** EN and UR. |
| Negative (discount) | `−Rs 35,625` | True minus (U+2212), not hyphen-minus. Colour via design tokens (`--success` for discounts). |
| Zero | `Rs 0` | |
| Whole rupees in UI | Parents and officers see **whole rupees** in MVP statements (paisa not shown). Storage remains `.00`. |

Portal and receipts use the same formatter. Do not invent `PKR`, `Rs.`, or `142500`.

### 1.4 Wire format

JSON amounts are **decimal strings** with exactly two fractional digits, e.g. `"142500.00"`. Never JSON numbers for money (IEEE float risk). OpenAPI: `type: string`, `format: decimal`, pattern `^-?\d+\.\d{2}$`.

### 1.5 Idempotency and snapshots

Payment attempts **snapshot** the payable amount and invoice version at creation (§6.4 of the plan). Later invoice adjustments do not mutate an in-flight attempt's amount.

---

## 2. Status vocabularies

**Canonical casing:** PascalCase strings on the wire and in DB enums / check constraints. Agents must not invent synonyms (`Complete` ≠ `Succeeded`). Design pills map **looks** only — words come from here (`DESIGN_SUPERVISOR.md` §5.3).

### 2.1 PaymentAttempt

```
Created → Pending → Succeeded | Failed | Expired | Cancelled
Succeeded → Reversed   (chargeback / bank reversal via reconciliation only)
```

| State | Meaning |
|---|---|
| `Created` | Attempt row written; adapter not yet called (or URL not yet returned). |
| `Pending` | Checkout / voucher / SDK flow in progress; awaiting verified callback or poll. |
| `Succeeded` | Verified success; ledger + receipt side-effects committed. |
| `Failed` | Verified failure or adapter hard-fail. |
| `Expired` | Timed out without success (poll / TTL job). |
| `Cancelled` | User or system cancelled before success. |
| `Reversed` | Post-success reversal only. |

Terminal for happy path: `Succeeded`. Do not use `Paid` on an attempt — that word belongs to Invoice.

### 2.2 Invoice

```
Draft → Issued → PartiallyPaid → Paid
Issued | PartiallyPaid → Superseded
```

| State | Meaning |
|---|---|
| `Draft` | Preview / not visible to parent as payable. |
| `Issued` | Immutable statement released; balance > 0, no payments yet (or payments not applied). |
| `PartiallyPaid` | Some but not all of balance collected. |
| `Paid` | Balance zero (credits may still exist separately). |
| `Superseded` | Replaced by adjustment / re-issue; never edited in place. |

**Derived (not stored):** `Overdue` — true when `due_date < today (tenant TZ policy: Asia/Karachi date)` **and** remaining balance > 0 **and** status ∈ {`Issued`, `PartiallyPaid`}. UI may show Overdue as a pill; APIs expose `isOverdue: boolean` and/or a computed `displayStatus` — never persist `Overdue` as `Invoice.status`.

Also derived for UI (not Invoice.status): **DueSoon** when due within ≤7 days and not overdue (`DESIGN_SUPERVISOR.md` §5.3).

### 2.3 Clearance (per student per term)

Stored / emitted as:

| State | Meaning |
|---|---|
| `Cleared` | Meets tenant clearance rule. |
| `ClearedWithDeferment` | Cleared because an approved deferment covers the rule. Parent label: "Cleared (deferment approved)". |
| `NotCleared` | Does not meet rule; outstanding amount and cut-off shown. |

Changes emit `ClearanceChanged` (§4).

### 2.4 Import job

```
Uploaded → Validating → Validated | FailedValidation
Validated → Committing → Succeeded | Failed
Succeeded → Reverted   (optional; most recent batch only, if supported)
```

| State | Meaning |
|---|---|
| `Uploaded` | File accepted; dry-run not finished. |
| `Validating` | Row validation running. |
| `Validated` | Dry-run complete; ready to commit (may include skip/error counts). |
| `FailedValidation` | Dry-run aborted or unusable (e.g. unreadable file). |
| `Committing` | Background commit in progress. |
| `Succeeded` | Commit finished. |
| `Failed` | Commit failed; see row errors. |
| `Reverted` | Batch reversed. |

### 2.5 Deferment

```
Requested → Approved | Rejected | Withdrawn
```

| State | Meaning |
|---|---|
| `Requested` | Parent/student submitted; awaiting finance. |
| `Approved` | Due date moved; late fee suppressed per policy. |
| `Rejected` | Declined with reason. |
| `Withdrawn` | Requester cancelled before decision. |

### 2.6 Forbidden status inventions

Do not add: `Processing`, `Complete`, `Active`, `Inactive`, `Open`, `Closed`, `TaxPending`, `WalletFunded`, or channel-specific parallel machines. All payment channels share §2.1.

---

## 3. Error-payload shape

**This is the contract that silently broke the prior A2Z build.** Every failed API response uses this envelope. Every client parses **only** this shape. Silent empty errors are a Gate failure.

### 3.1 Envelope

HTTP non-2xx bodies **must** be JSON:

```json
{
  "error": {
    "code": "PAYMENT_ATTEMPT_IN_PROGRESS",
    "message": "A payment is already in progress for this item.",
    "messageKey": "errors.payment_attempt_in_progress",
    "correlationId": "9f3c2e1a-4b5d-6789-abcd-ef0123456789",
    "details": [
      {
        "field": "instalmentId",
        "code": "CONFLICT",
        "message": "Resume the existing attempt instead of creating a new one.",
        "messageKey": "errors.field.conflict"
      }
    ],
    "retryable": false
  }
}
```

| Field | Type | Required | Rules |
|---|---|---|---|
| `error` | object | yes | Top-level key is always `error`. Never put `errors` / `title` / RFC7807-only without this wrapper. |
| `error.code` | string | yes | `SCREAMING_SNAKE_CASE`. Stable for clients to branch on. |
| `error.message` | string | yes | English human text safe to show if i18n missing. |
| `error.messageKey` | string | yes | i18n key; parent apps resolve EN/UR. |
| `error.correlationId` | string (UUID) | yes | Same id in logs/traces. |
| `error.details` | array | yes | Empty array if none. Never omit. |
| `error.details[].field` | string \| null | yes | Property path or `null` for non-field errors. |
| `error.details[].code` | string | yes | Field-level code. |
| `error.details[].message` | string | yes | English. |
| `error.details[].messageKey` | string | yes | i18n key. |
| `error.retryable` | boolean | yes | Clients may offer Retry when `true`. |

**Validation (400)** and **domain conflict (409)** use the same envelope. **401/403** still use it (no bare WWW-Authenticate-only UX). **500** uses a generic `message` / `messageKey`; never leak stack traces.

### 3.2 Examples

**Validation failure (400):**

```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "One or more fields are invalid.",
    "messageKey": "errors.validation_failed",
    "correlationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "details": [
      {
        "field": "mobileNumber",
        "code": "INVALID_FORMAT",
        "message": "Enter a Pakistani mobile number starting with 3.",
        "messageKey": "errors.mobile_invalid"
      }
    ],
    "retryable": false
  }
}
```

**OTP rate limit (429):**

```json
{
  "error": {
    "code": "OTP_RATE_LIMITED",
    "message": "Too many tries. Try again in 10 minutes.",
    "messageKey": "errors.otp_rate_limited",
    "correlationId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "details": [
      {
        "field": null,
        "code": "RETRY_AFTER",
        "message": "Retry after 600 seconds.",
        "messageKey": "errors.retry_after"
      }
    ],
    "retryable": true
  }
}
```

**Cross-tenant / not found (404) — same shape, no existence leak beyond "not found":**

```json
{
  "error": {
    "code": "RESOURCE_NOT_FOUND",
    "message": "We couldn't find that record.",
    "messageKey": "errors.resource_not_found",
    "correlationId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "details": [],
    "retryable": false
  }
}
```

### 3.3 Client obligations

- `packages/api-client` generated types **must** include this schema; hand-written error parsers are forbidden.
- Parent / portal UI **must** render `message` (or translated `messageKey`) plus a recovery action. Blank error regions fail Design Gate.
- Never swallow non-2xx without surfacing this payload.

### 3.4 Success envelope (non-error)

Success bodies are resource-shaped (no mandatory `{ data: ... }` wrapper in v1). Lists use:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0
}
```

OpenAPI documents each endpoint's 2xx schema explicitly.

---

## 4. Domain events

Written to the **outbox in the same DB transaction** as the state change (§5.4). Names are PascalCase. Payload amounts follow §1.4.

| Event | When | Payload outline (minimum) |
|---|---|---|
| `InvoiceIssued` | Invoice → `Issued` | `tenantId`, `invoiceId`, `studentId`, `termId`, `version`, `totalAmount`, `currency`=`PKR`, `dueDate`, `occurredAt` |
| `InvoiceSuperseded` | Invoice → `Superseded` | `tenantId`, `invoiceId`, `supersededByInvoiceId`, `occurredAt` |
| `PaymentAttemptCreated` | Attempt → `Created` | `tenantId`, `attemptId`, `payableType`, `payableId`, `amount`, `idempotencyKey`, `occurredAt` |
| `PaymentSucceeded` | Attempt → `Succeeded` | `tenantId`, `attemptId`, `paymentId`, `receiptId`, `receiptNumber`, `amount`, `channel`, `gatewayTxnId`, `studentId`, `invoiceId`, `occurredAt` |
| `PaymentFailed` | Attempt → `Failed` | `tenantId`, `attemptId`, `reasonCode`, `occurredAt` |
| `PaymentReversed` | Attempt → `Reversed` | `tenantId`, `attemptId`, `paymentId`, `amount`, `occurredAt` |
| `DefermentRequested` | Deferment → `Requested` | `tenantId`, `defermentId`, `studentId`, `itemType`, `itemId`, `proposedDueDate`, `occurredAt` |
| `DefermentApproved` | Deferment → `Approved` | `tenantId`, `defermentId`, `studentId`, `itemId`, `newDueDate`, `decidedByUserId`, `occurredAt` |
| `DefermentRejected` | Deferment → `Rejected` | `tenantId`, `defermentId`, `reason`, `decidedByUserId`, `occurredAt` |
| `ClearanceChanged` | Clearance status changes | `tenantId`, `studentId`, `termId`, `fromStatus`, `toStatus`, `outstandingAmount`, `cutOffDate`, `occurredAt` |
| `ImportJobCompleted` | Import → `Succeeded` \| `Failed` | `tenantId`, `jobId`, `entity`, `status`, `createdCount`, `updatedCount`, `skippedCount`, `errorCount`, `occurredAt` |
| `OfflinePaymentRecorded` | Manual/offline payment posted | `tenantId`, `paymentId`, `studentId`, `amount`, `reference`, `recordedByUserId`, `occurredAt` |
| `OverpaymentCredited` | Duplicate/overpay → student credit | `tenantId`, `studentId`, `creditId`, `amount`, `sourceAttemptId`, `occurredAt` |

**Naming rules:** past-tense domain fact (`PaymentSucceeded`, not `PaymentSuccess` / `OnPayment`). No tax events in P0–P1. Consumers must tolerate additive optional fields; removing or renaming a field is a contract bump.

Outbox row shape (implementation detail owned by A, names fixed here): `id`, `tenant_id`, `event_name`, `payload` (JSONB), `occurred_at`, `processed_at` nullable.

---

## 5. Reference / seed data — pilot tenant placeholder

**Purpose:** one anonymised source so agents do not invent conflicting banks, programs, or fee heads (§16.1).

**Rules:** no real CNICs, no real parent phone numbers, no real student names from pilot dumps. Use clearly fake placeholders. Values below are **the** pilot seed until S updates this section.

### 5.1 Tenant

| Field | Value |
|---|---|
| `tenant_id` (stable UUID) | `11111111-1111-1111-1111-111111111111` |
| `slug` | `pilot-u` |
| `display_name` | `Pilot University (Placeholder)` |
| `timezone` | `Asia/Karachi` |
| `locale_default` | `en` |
| `currency` | `PKR` |
| `brand_primary` | `#0B3D91` (identity only — never Pay button fill) |

### 5.2 Collection / banks (adapters config placeholders)

| Code | Display name | Tier hint | Notes |
|---|---|---|---|
| `raast-p2m-pilot` | Raast (Pilot Bank) | 1 | Credentials live in secret store; seed only references secret **names**, never values. |
| `jazzcash-pilot` | JazzCash | 1 | Wallet SDK / app-return. |
| `easypaisa-pilot` | Easypaisa | 1 | Wallet SDK / app-return. |
| `card-psp-pilot` | Card (PSP Hosted) | 2 | Hosted / certified SDK only — **no** PAN fields. |
| `biller-cn-pilot` | Consumer Number Biller | 3 | Fallback voucher. |

Secret name pattern: `tenant/{tenant_id}/adapters/{code}`. Seed rows store `secret_ref`, not secrets.

### 5.3 Programs (placeholder)

| Code | Name |
|---|---|
| `BSCS` | BS Computer Science |
| `BBA` | BBA |
| `BSSE` | BS Software Engineering |

### 5.4 Intakes / terms (placeholder)

| Code | Name |
|---|---|
| `FALL-2026` | Fall 2026 |
| `SPRING-2027` | Spring 2027 |

Intake example: `BSCS-2024` = "BS Computer Science — 2024 intake".

### 5.5 Fee heads (canonical codes)

| Code | Name | `refundable` | `taxable_for_236I` | `discountable` | `one_time` |
|---|---|---|---|---|---|
| `TUITION` | Tuition | true | true | true | false |
| `ADMISSION` | Admission | false | false | false | true |
| `REGISTRATION` | Registration | false | false | false | false |
| `EXAM` | Examination | false | false | false | false |
| `LAB` | Laboratory | false | false | true | false |
| `LIBRARY` | Library | false | false | false | false |
| `HOSTEL` | Hostel | true | false | false | false |
| `TRANSPORT` | Transport | false | false | false | false |
| `SECURITY` | Security Deposit | true | false | false | true |
| `REPEAT` | Repeat Course | false | true | false | false |

`taxable_for_236I` may be set in seed for future use. **No tax rules, tax lines, or tax UI** consume it in P0–P1 (§7.4 / §8).

### 5.6 Sample academic people (anonymised)

| Role | Identifier | Notes |
|---|---|---|
| Student | `STU-PILOT-0001` | Name: `Student One` · Program `BSCS` |
| Student | `STU-PILOT-0002` | Name: `Student Two` · Program `BBA` |
| Guardian mobile | `+923001111001` | Fake; OTP test numbers only in non-prod |
| Guardian mobile | `+923001111002` | Fake |
| Portal finance admin | `finance.admin@pilot-u.example` | Password only in secret store / local user secrets |

CNIC columns: leave null or use obviously invalid encrypted placeholders such as `00000-0000000-0` **only** in local/dev seeds — never production-like real numbers.

### 5.7 Ownership of seed application

Agent **A** applies seeds via migrations / seed runners. Agents **B/C/D/E/F** must not invent alternate bank codes or fee-head codes. If the pilot university supplies real anonymised extracts later, **S** updates §5 and notifies all agents.

---

## 6. Shared package API surfaces

Monorepo packages under `packages/*`. Public exports below are the contract; internals may change. TypeScript packages are built in P0 for web and later reused by native (L8).

### 6.1 `packages/money`

**Responsibility:** parse, compare, round, format PKR amounts. No React.

| Export | Contract |
|---|---|
| `Money` / branded string helpers | Construct from decimal string; reject float constructors. |
| `roundHalfUpPerLine(amount)` | Half-up to 2 dp. |
| `add`, `sub`, `sum` | Exact decimal arithmetic (string or Money in/out). |
| `compare(a, b)` | `-1 \| 0 \| 1`. |
| `formatPkr(amount, locale?)` | `Rs 142,500` / `−Rs 35,625` with thin space; Western digits for `en` and `ur`. |
| `assertPkrCurrency(code)` | Throws unless `PKR`. |

**Forbidden:** accepting `number` for money in public APIs (except integer minor units if explicitly named — MVP does **not** use minor units; prefer decimal strings).

### 6.2 `packages/validation`

**Responsibility:** shared zod (or equivalent) schemas aligned with API fields.

| Export | Contract |
|---|---|
| `mobilePk` | Pakistani mobile: stored/E.164 `+923XXXXXXXXX`; UI may show `3XX XXXXXXX`. |
| `otpCode` | 6 digits. |
| `decimalAmount` | Matches §1.4 pattern. |
| `uuid` | RFC UUID. |
| `pageParams` | `page` ≥ 1, `pageSize` capped. |
| `parseApiError(body)` | Returns typed §3 envelope or a safe fallback error object — **never throws away** unknown errors without a generic message. |

### 6.3 `packages/api-client`

**Responsibility:** **generated** OpenAPI client only.

| Rule | Detail |
|---|---|
| Generation | From backend OpenAPI document in CI; commit or artifact per A’s pipeline. |
| Hand-written shapes | **Forbidden.** |
| Auth | Inject bearer / cookie via configured middleware; no tokens in `localStorage` for native later; parent web follows A’s session design. |
| Errors | All error responses decoded with §3; exported type `ApiErrorEnvelope`. |
| Money fields | Typed as string decimal. |

S owns **content decisions** for schemas that appear in this contract (status enums, error schema, money format). A owns pipeline and generation mechanics.

### 6.4 `packages/ui`

**Responsibility:** tokens + shared primitives. Agent **A** implements; token **names** are fixed here; **values** live in `DESIGN_SUPERVISOR.md` §3 (S requests value changes there, does not fork values in feature apps).

#### 6.4.1 Colour token names

`--canvas`, `--surface`, `--surface-sunken`, `--ink`, `--ink-secondary`, `--ink-tertiary`, `--border`, `--border-strong`, `--accent`, `--accent-soft`, `--success`, `--success-soft`, `--warning`, `--warning-soft`, `--danger`, `--danger-soft`, `--on-ink`.

Dark theme variants use the same names under a dark scheme (values in DESIGN_SUPERVISOR).

Tenant brand is **not** a product action token; expose as `--tenant-brand` (identity chip/band only).

#### 6.4.2 Type token names

`display-amount`, `title-1`, `title-2`, `body`, `body-strong`, `label`, `micro`.

Font families: Lexend (headings), Source Sans 3 (body/UI/money), Noto Nastaliq Urdu (Urdu).

#### 6.4.3 Space token names

`space-2`, `space-4`, `space-8`, `space-12`, `space-16`, `space-20`, `space-24`, `space-32`, `space-40`, `space-48`, `space-64` (4-pt grid values per DESIGN_SUPERVISOR §3.4).

#### 6.4.4 Radius token names

`radius-8`, `radius-12`, `radius-16`, `radius-24`, `radius-full`.

#### 6.4.5 Shadow token names

`shadow-1`, `shadow-2`, `shadow-3`.

#### 6.4.6 Z-index token names

`z-base`, `z-sticky-header`, `z-sticky-action`, `z-sheet`, `z-dialog`, `z-toast` (map to 0 / 10 / 20 / 40 / 50 / 60).

#### 6.4.7 Motion token names

`--ease-out`, `--ease-in-out`, `--ease-drawer`, `--dur-press`, `--dur-fast`, `--dur-base`, `--dur-sheet`, `--dur-exit`.

Banned: `ease-in` for UI, `transition: all`, animating non-transform/opacity in routine UI.

#### 6.4.8 Required primitive exports (names)

`Button`, `StatusPill`, `Amount`, `Skeleton`, `EmptyState`, `ErrorState`, `BottomSheet`, `Toast` (or Sonner wrapper). `StatusPill` accepts **only** contract status / derived display statuses from §2 and §7 — no free-text status labels in product UI.

Feature apps must not copy-paste lookalike primitives (§16.1 design analogue).

---

## 7. Parent-friendly labels and i18n outline

Portal may show technical words; **parent mobile web / apps** use the mapping below. Status **enum values on the API stay PascalCase** from §2; UI maps via i18n keys.

### 7.1 Vocabulary mapping

| Domain / API word | Parent EN label | Notes |
|---|---|---|
| Invoice | **Statement** | Never "invoice" in parent UI copy. |
| PaymentAttempt | Payment | "Confirming with your bank…" while `Pending`. |
| `PartiallyPaid` | Partially paid | |
| `Paid` | Paid | |
| `Issued` | Due / Payable (context) | Prefer due date + amount; avoid "Issued". |
| Overdue (derived) | Overdue | |
| DueSoon (derived) | Due soon | |
| `Cleared` | Cleared | |
| `ClearedWithDeferment` | Cleared (deferment approved) | |
| `NotCleared` | Not cleared | |
| Deferment | Request more time | Screen title; status uses Approved / Rejected / Requested. |
| Fee head codes | Localised names from tenant | Never show `TUITION` raw. |
| Calculation rule ids | Hidden | Show trace text only. |

### 7.2 i18n key outline

Namespaces (EN + UR from day one on parent surfaces):

| Namespace | Examples |
|---|---|
| `status.invoice.*` | `status.invoice.paid`, `status.invoice.partially_paid`, `status.invoice.overdue` |
| `status.attempt.*` | `status.attempt.pending`, `status.attempt.failed` |
| `status.clearance.*` | `status.clearance.cleared`, `status.clearance.not_cleared`, `status.clearance.cleared_with_deferment` |
| `status.deferment.*` | `status.deferment.requested`, `status.deferment.approved`, `status.deferment.rejected` |
| `labels.statement` | "Statement" / Urdu equivalent |
| `labels.pay_amount` | "Pay {amount}" |
| `errors.*` | Matches `messageKey` in §3 |
| `money.prefix` | `Rs` (with thin space applied in formatter, not necessarily in string) |

Urdu: RTL layout; amounts remain Western digits (`DESIGN_SUPERVISOR.md` §3.3, §4.3).

---

## 8. Forbidden in this contract (scope creep kill-list)

The following are **explicitly out of contract** for P0–P1 (and until S removes them from this list). Agents must not add types, screens, events, seeds, or OpenAPI paths for them.

### 8.1 From plan §3.2

- Holding, pooling, splitting, forwarding money; **wallet balances** or user ledgers of held funds
- Storing card numbers, CVV, or bank credentials
- **Building our own card entry form** / PAN fields
- Embedding gateway checkout in a **WebView** (system browser tabs / certified SDKs only)
- Native Android/iOS **before** mobile web payment journey is proven (L8)
- Silent auto-debit or store-card-charge-later
- BNPL / lending logic (SNPL is partner hand-off later — no credit engine)
- LMS, attendance, exams management, timetabling, admissions (we read enrolment; we do not manage academics)
- AI features / chatbots
- School-specific MVP UI (monthly voucher printing, summer-fee guard)

### 8.2 From plan §7.4 (tax deferred)

- Tax rules configuration UI
- Tax lines on statements (including zero lines)
- ATL / exemption payer workflow for 236I
- Annual tax statement download
- Tax domain events
- Hard-coded tax rates

Allowed preparatory **only**: `TaxRule` table empty; `taxable_for_236I` flag on fee heads unused by engine output; reserved engine slot that emits **nothing** when no rules exist.

### 8.3 Tooling / money forbids (stack)

- `float` / `double` for money
- AutoMapper, MediatR, EPPlus
- `Database.Migrate()` at application startup
- Hand-written API client DTOs that duplicate OpenAPI

---

## 9. OpenAPI content decisions (S-owned)

Until the first generated document lands, agents must align designs to:

1. Status enums exactly as §2 (string enums).
2. Error schema exactly as §3 (`components.schemas.ApiError` + `ApiErrorDetail`).
3. Money as decimal strings (§1.4).
4. Event names in docs/webhooks matching §4.
5. No paths for §8 forbidden features.

A implements Swashbuckle/NSwag pipeline; **schema meaning** disputes escalate to S.

---

## 10. Change control

1. Agent opens a request under `docs/supervisor/REQUESTS_TO_*.md` (or equivalent).
2. S edits `CONTRACT.md`, bumps the version note in the header, and notifies affected agents.
3. No silent local forks of enums, error shapes, or seed codes.

**Contract ratified:** pending P0 supervisor gate (`docs/supervisor/PHASE_P0_LOG.md`).

---

*End of CONTRACT.md v1*
