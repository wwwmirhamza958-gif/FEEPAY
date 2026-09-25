# Fee Platform — Implementation Plan

| | |
|---|---|
| **Product** | Generic fee calculation + parent payment platform for universities, colleges and schools (working name TBD — see D1) |
| **Owner** | A2Z Creatorz — Technical |
| **Version** | v0.2 — DRAFT for stack finalisation |
| **Status** | Architecture proposed. Tech stack **proposed, not frozen** until §18 decisions are answered. Cursor workflows are written only after the freeze. |
| **Launch segment** | Universities (colleges and schools follow on the same engine) |

---

## 1. Locked decisions

These are settled. Every later section assumes them, and no agent may reopen them.

| # | Decision | Consequence for the build |
|---|---|---|
| L1 | **Money flow = Option A (direct settlement).** Parents pay into each institution's own gateway / collection bank. A2Z never holds, pools, forwards or splits funds. | No wallets, no balances held for users, no payouts, no commission deduction. The platform only computes amounts, initiates payments, receives confirmations and reconciles. |
| L2 | **Universities first.** | Credit-hour fee logic, semester billing, add/drop adjustments, repeat-course fees and exam-clearance holds are Phase-1 critical. School-only features (monthly vouchers, summer-fee guard) are engine-supported but not UI priorities. |
| L3 | **Data comes from both** Excel/CSV import **and** ERP integration. | A canonical data model plus two ingestion paths into it. Each tenant declares, per entity, whether the ERP or the platform is master. |
| L4 | **"Automatic payment" = pre-calculated amount + one-tap confirmation by the payer.** | No silent auto-debit. The parent always approves the payment in the gateway, banking app or wallet. |
| L5 | **Payer can be a parent/guardian or the student.** | Student accounts can pay; guardian linking is optional, not mandatory. |
| L6 | **Multi-institution family view.** One parent account sees all children across institutions. | Under L1 each institution is still paid in its own transaction. One screen, several checkouts. |
| L7 | **Payment happens inside our surface. Leaving for a bank is a fallback, not the design.** Simplification is the product; every extra hop loses payers. | Channels are ranked by how little context the payer loses (§6.3). Rails that inherently complete outside our surface are offered, but never as the first option shown. "Inside the app" means a certified provider SDK or a returning browser tab — **never our own card form** (§6.6). |
| L8 | **Build order: backend + parent mobile web first, then native Android and iOS.** The mobile web is structured so the native apps reuse it, not replace it. | Shared TypeScript packages for API client, money/format helpers, validation and design tokens are created in P0 and consumed by web first. Native app work starts only after the mobile web payment journey is proven end to end (§17). |
| L9 | **Parallel Cursor agents with a supervisor and continuous Bugbot testing.** | Every file has exactly one owning agent. No two agents touch the same screen or capability in the same phase. The supervisor gate must run to completion before a phase is declared done (§16). |

---

## 2. Problem → feature map

Pakistan-specific pains from the research, and the feature that answers each one. Priority column: **P1** = university MVP, **Later** = after pilot.

| Pain | Feature | Priority |
|---|---|---|
| Bank branch visits, queues, time off work | Pay without leaving our surface — Raast, wallet or card completed in-app; consumer-number voucher only as the fallback for payers who want a bank or agent | P1 |
| Payer abandons at the hand-off to a bank app or gateway page | Channels ranked by context loss (§6.3); the cheapest, fewest-hop option is the default shown | P1 |
| "Was my payment received?" calls | Server-confirmed status, instant WhatsApp/SMS receipt, live status on both sides | P1 |
| Opaque fee amounts, surprise hikes | Line-by-line statement with calculation trace + "what changed since last term" diff with % | P1 |
| Admit card / exam hold surprises | Clearance status visible to parent in advance + reminders before the hold date | P1 |
| Sibling, merit and need-based concessions tracked by memory | Discounts/scholarships stored as rules against the student, applied by the engine, shown on the statement | P1 |
| Section 236I advance tax confusion | Tax computed as a configurable rule, shown as a separate line, annual tax statement download | **Later** — engine slot built in P1, rules and UI deferred (§7.4) |
| Cash-flow pressure, lump sums | Institution-defined instalment plans; deferment / hardship request workflow | P1 (instalments), P1 (deferment) |
| Someone else pays (relative, parent abroad) | Pay-on-behalf link for a single invoice | P1 |
| Refund confusion (only tuition refundable, sliding timelines) | Refund request tracker against the institution's configured policy | Later |
| Non-digital payers excluded | Consumer-number voucher payable at any bank / agent (biller model) | P1 |
| Informal borrowing for fees | Study-now-pay-later partner hand-off at checkout | Later |
| Advance multi-month collection, hikes above caps (mostly schools) | Compliance guard: configurable policy checks before invoices are issued | Later (schools) |
| Overseas payer 236I exemption paperwork | Exemption document upload + finance approval | Later |

---

## 3. Scope

### 3.1 In scope — University MVP
1. Multi-tenant platform: one tenant per institution, white-label branding per tenant.
2. Institution portal: setup, imports, fee schedules, discounts, statement generation, collections dashboard, defaulters, clearance, deferments, reports.
3. Fee engine: credit-hour and flat rules, heads, discounts, tax, late fees, instalments, adjustments, explainable output.
4. Data ingestion: Excel/CSV import wizard + ERP connector framework with one real connector for the pilot university.
5. Parent/student experience: **parent mobile web first**, then Android + iOS apps built on the same API and shared packages (L8). Payment links from WhatsApp must work without an install.
6. Payments: adapter layer, ranked by context loss (§6.3) — instant-rail and wallet adapters completed in-app, hosted-checkout adapter, and biller/consumer-number adapter as the fallback; receipts; reconciliation.
7. Notifications: WhatsApp, SMS, email, push; English + Urdu.
8. Platform admin (A2Z): tenant onboarding, support tools, audit.

### 3.2 Explicitly out of scope — agents must NOT build
- Any holding, pooling, splitting or forwarding of money (see L1). No wallet balances.
- Storing card numbers, CVV or bank credentials. Hosted pages or certified provider SDKs only.
- **Building our own card entry form**, however native it looks. "In-app payment" (L7) is delivered by the provider's certified SDK, never by us collecting a PAN. Doing so pulls A2Z and every tenant into PCI DSS scope.
- Embedding gateway checkout inside a WebView (use system browser tabs — §6.6).
- Tax rules, tax lines on statements, or the annual tax statement — deferred out of P1 (§7.4). The engine's rule slot exists; nothing is configured or displayed.
- Native Android/iOS app work before the mobile web payment journey is proven end to end (L8).
- Silent auto-debit or "stored card, charge later" flows.
- BNPL / lending logic. The later SNPL feature is a hand-off link to a licensed partner, not credit logic.
- LMS, attendance, exams, timetabling, admissions. We read enrolment data; we don't manage academics.
- AI features, chatbots.
- School-specific UI (monthly voucher printing, summer-fee guard) in the MVP.

---

## 4. Users and roles

| Actor | Where | Core permissions |
|---|---|---|
| Parent / Guardian | App + mobile web | View linked students' statements, pay, download receipts and tax statement, request deferment, share pay link |
| Student | App + mobile web | Same as parent for own record; can link a guardian |
| Pay-on-behalf payer | Mobile web only (no account) | Pay one specific invoice through a signed, expiring link |
| Finance Admin (institution) | Portal | Everything in the tenant: fee schedules, discounts, policies, users, integrations, approvals |
| Finance Officer | Portal | Generate/issue statements, record manual/offline payments, handle deferments, run reconciliation |
| Exam / Registrar Cell | Portal (read-mostly) | View and export clearance status |
| Institution IT | Portal | Imports, ERP connector config, API keys, webhooks |
| Auditor (institution) | Portal | Read-only + audit log |
| A2Z Platform Admin | Platform admin | Create tenants, feature flags, support impersonation (logged + time-boxed), global audit |

RBAC is permission-based (roles are named bundles of permissions), same pattern as prior A2Z builds.

---

## 5. Architecture

### 5.1 Shape
A **modular monolith** backend (one deployable, strict module boundaries) plus background workers, three web front-ends and one mobile app. Microservices are deliberately avoided at this stage.

```
                 ┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐
                 │ Parent / Student │   │ Parent mobile web│   │ Institution      │
                 │ app (iOS/Android)│   │ + pay-link pages │   │ portal           │
                 └────────┬─────────┘   └────────┬─────────┘   └────────┬─────────┘
                          │  generated OpenAPI clients (all front-ends) │
                          └───────────────┬──────┴───────────────────────┘
                                          ▼
 ┌───────────────────────────────── API (modular monolith) ─────────────────────────────────┐
 │ Identity & Tenancy │ Academic data │ Fee Engine │ Billing (invoices) │ Payments │ Notify │
 │ Integrations (import + ERP) │ Clearance │ Reporting │ Audit │ Platform admin            │
 └───────────────┬──────────────────────────────┬───────────────────────────────┬──────────┘
                 ▼                              ▼                               ▼
          PostgreSQL (tenant-scoped)     Job runner + outbox            Object storage
                                          (reminders, recon,             (receipts, imports,
                                           late fees, sync)               tax statements)
                 │                              │
                 ▼                              ▼
   Payment adapters (per tenant)       Messaging providers
   • Tier 1: Raast P2M, wallets        • WhatsApp BSP • SMS aggregator
   • Tier 2: card via PSP SDK/hosted   • Email • Push
   • Tier 3: biller / consumer number
                 │
                 ▼
   Institution's OWN merchant / collection account   ← money never touches A2Z
```

### 5.2 Module boundaries
Each module owns its tables. Other modules call it through an internal interface or react to its domain events. No module queries another module's tables directly.

| Module | Owns |
|---|---|
| Identity & Tenancy | tenants, users, roles, permissions, OTP, sessions, devices |
| Academic Data | programs, intakes/batches, terms, courses, students, guardians, enrolments |
| Fee Engine | fee schedules, fee heads, rules, discount rules, tax rules, late-fee rules (pure calculation, no I/O) |
| Billing | invoices (statements), line items, instalments, adjustments/credit notes, deferments |
| Payments | payment attempts, gateway transactions, receipts, reconciliation runs |
| Clearance | clearance status per student per term |
| Integrations | import jobs, ERP connector configs, field mappings, sync logs, outbound webhooks |
| Notifications | templates, schedules, deliveries |
| Reporting | read models and exports |
| Audit | append-only audit events |

### 5.3 Multi-tenancy
- **Default:** shared database, every tenant-owned row carries `tenant_id`. Isolation is enforced **twice**: an EF Core global query filter in the application, and PostgreSQL row-level security in the database, keyed on a per-connection tenant setting.
- **Enterprise tier (later):** a tenant can be moved to its own database. Build a tenant → connection-string resolver from day one so this is configuration, not a rewrite.
- Every background job runs inside an explicit tenant context. A job without a tenant context fails closed.
- Platform-admin queries across tenants go through a separate, audited path.

### 5.4 Domain events and the outbox
State changes that other modules or external systems care about (`InvoiceIssued`, `PaymentSucceeded`, `DefermentApproved`, `ClearanceChanged`) are written to an **outbox table in the same transaction** as the change, then dispatched by a worker. This guarantees an ERP webhook or WhatsApp receipt is never lost and never sent for a change that rolled back.

---

## 6. Payments (Option A)

### 6.1 Principle
The platform is the **initiator and the record-keeper**, never the receiver. Each tenant connects its own merchant or collection account. A2Z's credentials never appear in a settlement path.

### 6.2 Payment adapter interface
Every payment channel implements one interface. The core never knows which gateway it is talking to.

```
interface IPaymentAdapter
  Capabilities                     // HostedCheckout, ConsumerNumber, RequestToPay, Refund, StatusQuery, SettlementReport
  CreateCheckout(attempt)          -> redirect URL + gateway reference
  GetStatus(gatewayRef)            -> normalised status
  VerifyAndParseCallback(request)  -> normalised result (MUST verify signature/hash)
  FetchSettlementReport(date)      -> normalised lines (for reconciliation)
  RequestRefund(txn, amount)       -> optional; many institutions refund offline
```

Adapters are configured **per tenant** (credentials in the secret store, never in the database in plain text). A tenant may enable several adapters at once (e.g. card gateway + consumer-number biller).

### 6.3 Channel tiers — ranked by context loss and cost

L7 makes simplification the product, so channels are ranked by **how much context the payer loses** and what the transaction costs. The default channel shown to a payer is the highest tier their tenant has enabled.

| Tier | Channel | Payer experience | Cost | Status |
|---|---|---|---|---|
| **1** | **Raast P2M** — instant rail, via the institution's acquiring bank or a PSP | Approves in-app or in one returning hop; funds settle instantly | **Lowest by far** — SBP has driven P2M acceptance charges to zero or near-zero | Primary. Confirm onboarding route with the pilot's bank (D12) |
| **1** | **Wallets** — JazzCash, Easypaisa | Provider SDK completes in-app, or app-to-app hand-off and return | Low | Primary for payers without a bank account |
| **2** | **Card via PSP** — certified drop-in SDK preferred, hosted page otherwise | SDK renders natively in-app; hosted page opens a system browser tab and returns | **Highest** — MDR is a percentage, so it scales with the fee | Offer, but never default when a Tier 1 channel is available |
| **3** | **Consumer number / biller (1Link, Kuickpay-style)** | Voucher shown in-app; payer pays in **their own** bank app, ATM, agent or branch | Low flat fee | Fallback for payers who prefer a bank, and for offline/rural coverage |

**Why Tier 1 leads.** On a PKR 200,000 semester fee, a card at roughly 2% MDR costs someone about **PKR 4,000 per payment**. Across a 5,000-student university billing twice a year that is a very large recurring number, and whoever absorbs it — the institution or the parent — it decides whether the product is adopted. Defaulting to cards would be the single most expensive design choice in this plan.

**What the biller rail can and cannot do.** The consumer-number model sits on the 1Link bill-payment rail, where the debit happens in the **payer's** bank app, ATM or branch. That is the design of the rail, not a limitation of our app. We can make it feel integrated — generate the voucher in-app, show live status, auto-reconcile on the aggregator's callback — but we must not promise that the payment step itself happens in our surface. If an aggregator offers a hosted checkout that completes without leaving us, it moves to Tier 2; confirm with the pilot's aggregator before building (D13).

**Agent rule:** the Pay screen shows the tenant's highest enabled tier as the default and presents lower tiers under "Other ways to pay". Never render all four as an undifferentiated list — that is the hand-off problem L7 exists to solve.

### 6.4 Payment flow (hosted checkout)
1. Parent taps **Pay** on an invoice or instalment.
2. API creates a `PaymentAttempt` with an idempotency key, **snapshotting the invoice version and amount**. Only one active attempt per payable item; a second tap resumes the existing attempt.
3. Adapter returns the redirect URL. Client opens it in a **system browser tab**.
4. Gateway posts the callback to the API. The API verifies the signature, matches the attempt, and records the gateway transaction with a **unique constraint on (adapter, gateway_txn_id)** so replays are harmless.
5. On success, in one transaction: attempt → Succeeded, invoice/instalment paid amount updated, receipt number allocated, outbox events written.
6. Client return URL shows status by **asking the API**. The return URL itself is never trusted as proof of payment.
7. If no callback arrives, a job polls `GetStatus` on a back-off schedule until the attempt resolves or expires.

### 6.5 Status models

```
PaymentAttempt: Created → Pending → Succeeded | Failed | Expired | Cancelled
                Succeeded → Reversed            (chargeback / bank reversal, via reconciliation)

Invoice:        Draft → Issued → PartiallyPaid → Paid
                Issued|PartiallyPaid → Superseded   (replaced by an adjustment, never edited)
                Overdue is DERIVED (due date passed and balance > 0), not stored as a state
```

### 6.6 Rules the agents must follow
- **"In-app" means a certified provider SDK, not our own form.** Where a provider ships a drop-in or tokenising SDK, use it. Where it does not, a returning system browser tab is the in-app equivalent. Under no circumstances do we render a card-number field of our own — that pulls A2Z and every tenant into PCI DSS scope.
- Hosted checkout opens in Chrome Custom Tabs / SFSafariViewController (or the platform equivalent), **never a WebView**. Return via deep link / universal link.
- Every channel resolves through the same `PaymentAttempt` state machine (§6.5) regardless of tier. A Tier 3 voucher payment and a Tier 1 instant payment produce the same ledger entry, the same receipt and the same outbox events. No channel gets its own parallel path.
- Callbacks are verified before anything is written. An unverified callback is logged and rejected.
- Money in the database is `numeric(18,2)` / C# `decimal`. `float`/`double` for money is forbidden. Currency is PKR only in the MVP.
- A payment that arrives for an already-paid item (e.g. a consumer-number payment made twice) is recorded as an **overpayment credit** on the student account and flagged to finance. It is never silently dropped.
- Offline payments (cash at the university counter, bank draft) are recorded by a Finance Officer with a reference and appear identically in the ledger.

### 6.7 Reconciliation
Daily job per tenant per adapter: fetch the settlement report, match lines to gateway transactions, and flag three exception types for finance: **paid at gateway but not in platform**, **in platform but not settled**, and **amount mismatch**. Finance resolves each exception with a logged action. Dashboard shows collected vs settled vs outstanding.

### 6.8 Receipts
Receipt numbers come from a per-tenant gap-free sequence. The receipt PDF carries institution branding, line items paid, tax collected, payment channel, gateway reference and a QR code that opens a public verification page (shows validity, amount and date only — no personal details beyond initials).

---

## 7. Fee engine

The engine is the core product. It is a **pure calculation library**: inputs in, statement out, no database or network calls inside it. That makes it fully unit-testable and lets every tenant preview a change before issuing.

### 7.1 Concepts

| Concept | Meaning |
|---|---|
| Term | A billing period: semester, trimester, year, or month (schools) |
| Fee schedule | A **versioned** set of rates with effective dates, attached to program + intake/batch. Universities often freeze rates per intake, so the intake is a first-class key. |
| Fee head | A named charge line: Tuition, Admission, Registration, Exam, Lab, Library, Hostel, Transport, Security Deposit, Repeat-course, Summer term, etc. Each head carries flags: `refundable`, `taxable_for_236I`, `discountable`, `one_time`. |
| Rule | How a head's amount is computed (types in 7.2) |
| Discount rule | Scholarship / concession applied to specific heads |
| Tax rule | Section 236I and any future levy, parameter-driven |
| Late-fee rule | Charge applied after a due date |
| Instalment plan | How an invoice total is split across due dates |

### 7.2 Rule types (MVP)

| Rule type | Example |
|---|---|
| `PerCreditHour` | Registered credit hours × rate (rate may vary by course category, e.g. lab vs theory) |
| `PerCourse` | Flat amount per registered course |
| `PerRepeatCourse` | Separate rate for repeated/improvement courses |
| `FlatPerTerm` | Fixed per semester |
| `FlatMonthly` | Fixed per month (schools, some colleges) |
| `OneTime` | Charged once, with a condition (e.g. first term only) |
| `Conditional` | Applies when a student attribute matches (hostel resident, transport route, evening program) |

Rules take parameters, never hard-coded numbers. Adding a rule type later must not require schema changes to existing rules (rule parameters stored as validated JSON against a versioned schema per rule type).

### 7.3 Discounts and scholarships
- Types: percentage or fixed amount; target specific heads (commonly tuition only).
- Categories: merit, need-based, sibling, staff child, sports, external sponsor.
- Explicit **stacking policy** per tenant: which categories stack, in what order, and a maximum total discount.
- Validity: term range, and optional conditions (e.g. minimum GPA) that the finance team confirms each term — the engine does not read grades itself.

### 7.4 Tax (Section 236I) — DEFERRED out of Phase 1

**Not built in the MVP.** Tax rules, tax lines on statements, the payer ATL/exemption workflow and the annual tax statement all move to Later.

What P1 **does** build, so that adding tax later is configuration rather than surgery:

- `TaxRule` stays in the data model (§15) with its parameter JSONB, but no rules are configured and no UI exists to configure them.
- Fee heads keep the `taxable_for_236I` flag. Finance can set it during setup; nothing consumes it yet.
- The engine's statement output reserves a tax line position. With no rules configured it emits nothing — not a zero line.
- `PayerProfile` keeps its encrypted CNIC/NTN columns. They are captured only where already needed for identity, not for tax.

**When tax is picked up:** rates and thresholds must be confirmed with the pilot university's finance office or tax adviser, and the engine must never ship with a hard-coded rate. Institutions handling 236I today continue to do so in their existing process during the pilot — confirm this is acceptable to the pilot university before P1 closes, because it is a live statutory obligation for them (R8, §20).

### 7.5 Late fees, instalments, deferments
- Late fee: fixed, per-day, or per-week, with an optional cap, applied by a daily job as an **adjustment line**, never by editing the original invoice.
- Instalments: tenant defines plans (e.g. 50/50, 40/30/30) per program; the parent sees each instalment with its own due date and Pay button.
- Deferment request: parent submits a reason and proposed date → Finance approves/rejects → on approval, due date moves and late fee is suppressed for that item. Clearance status respects approved deferments.

### 7.6 Outputs and integrity
- Output is a **statement** with line items. **Every line carries a human-readable calculation trace** (e.g. `15 credit hours × Rs 9,500 = Rs 142,500`, `Merit scholarship 25% of tuition = −Rs 35,625`). The parent sees this trace.
- Issued invoices are **immutable**. Any change (add/drop, scholarship granted late, correction) produces an **adjustment invoice or credit note** linked to the original. This keeps the ledger auditable.
- **Term-over-term diff:** each statement stores a comparison with the same student's previous term: per-head change and total % change. Shown to the parent.
- Rounding: per line, to whole rupees, half-up, documented in one place.
- **Preview mode:** finance can run the engine against all students for a term without issuing, see totals and outliers, then issue.

### 7.7 Testing requirement
A golden-fixture test suite: realistic student scenarios (full load, overload, add/drop, repeat courses, scholarship stacking, instalments, late fee) with expected statements committed as JSON. Any engine change must pass all fixtures. This is non-negotiable before the payments phase. Tax fixtures are added when tax is picked up (§7.4).

---

## 8. Data ingestion

### 8.1 Canonical model
Both ingestion paths write into the same canonical entities: programs, intakes, terms, courses, students, guardians, enrolments, opening balances, and (optionally) fee schedules. Every imported record keeps its **external ID** from the source so re-imports and syncs upsert rather than duplicate.

### 8.2 Source-of-truth mode (per tenant, per entity)
Each tenant sets, per entity, **ERP-mastered** or **Platform-mastered**. An ERP-mastered entity is read-only in the portal (edits happen in the ERP and sync in). This prevents two systems overwriting each other.

### 8.3 Excel / CSV import wizard
1. Download the template for the entity (with an instructions sheet and example rows).
2. Upload → column mapping (auto-matched, adjustable, saved per tenant).
3. **Dry run**: row-level validation, duplicate detection, reference checks (e.g. enrolment references a course that exists). Shows counts to create / update / skip / error.
4. Download the error report, fix, re-upload, or commit the valid rows.
5. Commit runs as a background job; the result is logged and reversible for the most recent batch.

### 8.4 ERP connector framework
- A connector implements: `Pull(entity, since)` for inbound and `Push(event)` for outbound.
- Inbound modes: scheduled API pull, scheduled read of an agreed database view, or the ERP pushes to our REST API.
- Outbound: payment posted, invoice issued, clearance changed → sent to the ERP by its API or by our signed webhooks.
- Field mapping and transforms are configuration per tenant, not code, where possible.
- Every sync run is logged with counts and errors; failures retry with back-off and alert Institution IT.
- **MVP builds the framework + one real connector for the pilot university.** Generic REST API + webhooks let any ERP integrate without a custom connector.

### 8.5 Public API for institutions
Versioned REST API with tenant-scoped API keys (hashed at rest, scoped permissions, rotatable) so institution IT teams can push data and read payment status themselves.

---

## 9. Parent / student experience

### 9.1 Identity and linking
- Sign-up and login by **mobile number + OTP**. No passwords for parents.
- **Linking (recommended default):** the institution's data includes the guardian's mobile number (and optionally CNIC). When a parent signs up with that number, their students appear automatically.
- **Fallback:** the student generates a one-time link code in their own account and shares it with the guardian.
- **Not allowed:** looking up a student's fees by roll number alone. Fee data is personal financial data and must not be discoverable.
- One parent account can hold students across **multiple institutions** (L6).

### 9.2 MVP screens
1. OTP login
2. Home: all linked students, grouped by institution, with total due and next due date
3. Student statement: line items with calculation traces, term-over-term diff, instalments, clearance status
4. Pay: choose instalment/amount → default channel is the tenant's highest enabled tier, lower tiers under "Other ways to pay" (§6.3) → status
5. Payment history + receipt download
6. Deferment request + status
7. Share pay-on-behalf link
8. Notification settings, language (English / Urdu), profile

Tax statement (annual) is **not** an MVP screen — tax is deferred (§7.4).

### 9.3 Mobile web — built first (L8)
The parent mobile web is the **first** parent surface delivered, not a companion to the app. It carries the full journey: OTP login, statement, Pay, receipts. The native apps are built afterwards on the same API and the same shared packages.

Every payment reminder carries a link that opens the statement and Pay flow in the phone's browser, with OTP login, **without requiring the app**. The pay-on-behalf page works with no account at all and shows only: institution, student initials, term, amount, due date.

### 9.4 Language
i18n from day one. English + Urdu (right-to-left layout) for all parent-facing surfaces. Portal ships in English first.

---

## 10. Institution portal (MVP)

1. **Setup:** branding, terms calendar, programs/intakes, fee heads, fee schedules (versioned), discount categories + stacking policy, late-fee rules, instalment plans, payment adapters, notification templates.
2. **Data:** import wizard, sync status, student search, student 360 (enrolments, statements, payments, discounts, deferments, messages sent).
3. **Billing:** preview run → issue statements for a term; adjustments/credit notes; manual/offline payment entry.
4. **Collections dashboard:** issued vs collected vs settled, by program/intake, trend by day around deadlines.
5. **Defaulters:** aging buckets, filters, bulk reminder, export.
6. **Deferments:** queue with approve/reject and notes.
7. **Clearance:** list per term, export for exam cell, API/webhook to ERP.
8. **Reconciliation:** exceptions queue (§6.7).
9. **Reports:** collections, discounts granted, overpayments, adjustments. Export to Excel/CSV/PDF. (Tax collected / withholding statement — deferred, §7.4.)
10. **Users & roles, API keys, webhooks, audit log.**

---

## 11. Exam clearance
Per student per term, derived status: **Cleared**, **Cleared (deferment approved)**, **Not cleared**, with the outstanding amount and the clearance cut-off date. Rules are tenant-configurable (e.g. cleared when first instalment is paid). Parents see the status and cut-off date in advance; reminders escalate as the cut-off approaches. Changes emit `ClearanceChanged` for the ERP.

---

## 12. Notifications

| Trigger | Default channels |
|---|---|
| Statement issued | WhatsApp + push + email |
| Reminder T-7, T-3, T-1, due day | WhatsApp + push |
| Overdue / clearance at risk | WhatsApp + SMS |
| Payment succeeded (receipt) | WhatsApp + push + email |
| Deferment decided | WhatsApp + push |
| Adjustment issued | WhatsApp + push |

- Templates per tenant, bilingual, with variables. WhatsApp templates need provider pre-approval, so they are defined early (Phase 2).
- Quiet hours and per-parent channel preferences.
- Every delivery logged with provider status.
- Providers sit behind an interface (WhatsApp BSP, SMS aggregator, email, push) so they can be swapped.

---

## 13. Security, privacy, audit
- **Auth:** OTP for parents/students; email + password + TOTP 2FA (mandatory for Finance Admin) for portal users. Short-lived access tokens, rotating refresh tokens, revocation on logout/role change.
- **OTP abuse:** rate limits per number, per IP and per device; lockout windows; OTP cost monitoring.
- **Tenant isolation:** double enforcement (§5.3) + automated cross-tenant access tests in CI.
- **Data:** CNIC/NTN and phone numbers encrypted at the column level; TLS everywhere; secrets in a managed secret store (never in config files or the repo).
- **Payments:** no card data ever touches our systems; callback signature verification; replay protection.
- **Audit:** append-only log of every financial action (issue, adjust, waive, approve deferment, manual payment, recon resolution), every permission change and every support impersonation.
- **Standard web hardening:** OWASP ASVS-aligned checklist, dependency scanning, rate limiting on public endpoints, signed pay-links with expiry.
- **Retention:** financial records retained per the institution's policy and applicable law; configurable per tenant.

---

## 14. Proposed technology stack — for finalisation

Rationale: reuse what the A2Z team already runs in production (the OBS project established .NET + React Native + generated OpenAPI clients), and pick PostgreSQL because this is our own multi-tenant SaaS rather than a client's on-prem SQL Server.

| Layer | Proposed | Why | Alternative |
|---|---|---|---|
| Backend | **.NET 10 (LTS), ASP.NET Core Web API**, modular monolith | Team already has a .NET developer; strong typing and `decimal` for money logic | NestJS (TypeScript end-to-end), Laravel |
| ORM / DB | **EF Core 10 + PostgreSQL** (pin exact version at kickoff) | Row-level security for tenant isolation, JSONB for rule params and connector configs, no per-core licensing in a SaaS | SQL Server (licensing cost at scale) |
| Jobs | **Hangfire** on PostgreSQL storage + outbox dispatcher | Reminders, late fees, recon, polling, sync; one less piece of infrastructure than a message broker | Quartz.NET; add RabbitMQ only if load demands |
| Cache / rate limit | Redis | OTP throttling, rate limits, short-lived caches | In-memory (single node only) |
| Validation / docs | FluentValidation; OpenAPI via Swashbuckle or NSwag | Same as OBS | — |
| Files | ClosedXML (Excel import/export), CsvHelper, QuestPDF (receipts, statements) | Same as OBS | — |
| Logging / tracing | Serilog + OpenTelemetry → Seq or Grafana stack | Needed to trace a payment across callback, job and webhook | — |
| Web front-ends | **Next.js + TypeScript + Tailwind + shadcn/ui**, three apps in one **pnpm + Turborepo** monorepo: `institution-portal`, `parent-web`, `platform-admin`, sharing `packages/ui` and a generated `packages/api-client` | One design system, one generated client | Razor (portal only) — weaker for the public parent web |
| Mobile | **React Native + TypeScript**, generated OpenAPI client, secure token storage (Keychain/Keystore) | Team skill from OBS; one codebase for Android + iOS | Flutter |
| Mobile toolchain | **Expo (dev client / prebuild) recommended** — OTA updates, push, deep links are simpler for a consumer app | See D3 | Bare React Native (matches OBS pins) |
| Object storage | S3-compatible bucket | Receipts, statements, import files | — |
| CI/CD | GitHub Actions; Docker images | — | — |
| Hosting | Containers on a cloud or Pakistan data-centre provider | See D5 | — |

**Carried over from OBS as forbidden:** hand-written API request shapes (use generated clients), tokens in AsyncStorage/localStorage, AutoMapper, MediatR, EPPlus, `Database.Migrate()` at startup, `float`/`double` for money.

---

## 15. Core data model (outline)
All tables below except platform tables carry `tenant_id`. Money is `numeric(18,2)`. Timestamps are UTC.

**Identity & tenancy:** Tenant, TenantBranding, User, Role, Permission, RolePermission, UserRole, PayerProfile (CNIC/NTN enc., ATL declaration), OtpChallenge, Session, ApiKey.
**Academic:** Program, Intake, Term, Course (credit hours, category), Student (external_id, program, intake, attributes JSON), Guardian, StudentGuardian (relation, verified), Enrolment (student, course, term, is_repeat, status, added/dropped dates).
**Fee engine config:** FeeHead (flags), FeeSchedule (program, intake, effective_from/to, version), FeeRule (head, rule_type, params JSONB, schema_version), DiscountCategory, StackingPolicy, StudentDiscount (category, value, heads, term range), TaxRule (params), LateFeeRule, InstalmentPlan, InstalmentPlanPart.
**Billing:** Invoice (student, term, version, status, totals, previous_invoice_id, diff JSON), InvoiceLine (head, amount, trace text, rule ref), Instalment (invoice, due date, amount, paid), Adjustment (original invoice, reason, lines), Deferment (item, requested/approved dates, status, decided_by), StudentCredit (overpayments).
**Payments:** PaymentAdapterConfig (type, secret ref, enabled), PaymentAttempt (item, amount snapshot, invoice version, idempotency key, status), GatewayTransaction (adapter, gateway_txn_id UNIQUE, raw payload ref), Payment (ledger entry incl. offline), Receipt (number, pdf ref), ReconciliationRun, ReconciliationException.
**Other:** ClearanceStatus, ImportJob, ImportRowError, ConnectorConfig, FieldMapping, SyncRun, Webhook, WebhookDelivery, NotificationTemplate, NotificationDelivery, PayLink (signed, expiry, invoice), OutboxMessage, AuditEvent.

The full column-level schema is produced after the stack freeze, as a separate review document for the backend developer (same approach as OBS).

---

## 16. Development model — parallel agents, one supervisor

Several Cursor agents build different modules simultaneously, a supervisor agent polices the boundaries, and Bugbot tests continuously alongside (L9).

### 16.1 The failure this is designed to prevent

A previous A2Z build ran two agents in parallel on an ERP. The code compiled, the tests passed, and the modules worked. What went wrong was **quieter than a merge conflict**: the two agents seeded contradictory bank details, an incorrect buyer address, and duplicate article numbers where the source documents had two distinct ones. An error-payload shape disagreed between the API and the UI, so a blocked action silently produced no message at all. The supervisor pass that would have caught all of it was started and abandoned.

The lesson is specific and it shapes everything below. **Bugbot catches broken code. It does not catch two agents holding contradictory beliefs about the truth.** Anything shared — reference data, contracts, enums, money rules, status vocabularies — needs a single owner and a gate, because no test will fail when two agents each write something internally consistent and mutually incompatible.

### 16.2 Ownership — one file, one agent

Every path in the repository has exactly one owning agent per phase. An agent that needs a change in another agent's path **requests it**; it does not edit it.

| Agent | Owns | Never touches |
|---|---|---|
| **A — Platform** | Tenancy, identity, RBAC, audit, outbox, migrations infrastructure, CI, shared TS packages (`api-client`, `ui`, `money`, `validation`) | Any feature module's tables or screens |
| **B — Fee engine** | The pure calculation library, rule types, golden fixtures | Anything with I/O; any screen |
| **C — Billing & data** | Invoices, instalments, adjustments, deferments, import wizard, ERP connector framework | Payments tables; the engine internals |
| **D — Payments** | Adapters, `PaymentAttempt`, gateway transactions, receipts, reconciliation | Invoice/instalment tables — it calls Billing's interface |
| **E — Institution portal** | Portal screens and their API endpoints | Parent surfaces |
| **F — Parent web** | Parent mobile web screens and their API endpoints | Portal screens |
| **G — Native apps** | Android/iOS shells and native-only concerns, **after** L8's web gate | Any API; any shared package |
| **S — Supervisor** | `CONTRACT.md`, seed data, shared enums, the OpenAPI contract, cross-module review | Feature code — it reviews and requests, it does not implement |

**Hard rule: no two agents work on the same screen or capability in the same phase.** Where a phase would require it, the phase is split rather than the ownership shared.

### 16.3 The shared contract

`CONTRACT.md` at the repository root is **owned solely by the supervisor**. It holds everything more than one agent depends on:

- Money rules — `numeric(18,2)`, rounding half-up per line, PKR only
- Status vocabularies — `PaymentAttempt`, `Invoice`, clearance, import job states
- The error-payload shape every API returns and every client parses *(this is the exact thing that silently broke on the prior build)*
- Domain event names and payloads
- Reference and seed data — the pilot tenant, banks, programs, fee heads
- Shared package APIs

An agent that needs a contract change opens a request. The supervisor decides, edits `CONTRACT.md`, and notifies every affected agent. **An agent that edits `CONTRACT.md` directly has its work rejected for that phase** — the enforcement has to be blunt or it will be worked around.

### 16.4 Supervisor gate — must run to completion

No phase is done until the supervisor has completed, in order:

1. **Ownership audit** — every changed file belongs to the agent that changed it.
2. **Contract conformance** — status values, error shapes, event names and money handling match `CONTRACT.md` across every module that touches them.
3. **Seed and reference reconciliation** — no two agents have written conflicting reference data. Checked against the source documents, not against each other.
4. **Cross-module trace** — one real journey followed end to end through every module it crosses (in P3: statement issued → attempt created → callback verified → ledger written → receipt issued → outbox dispatched → reconciliation matched).
5. **Deferred-scope check** — nothing from §3.2 has been built, and tax has not crept back in (§7.4).
6. **Written sign-off** appended to the phase log, naming what was checked.

An abandoned or partial supervisor pass means the gate has **not** been passed. This is not a formality; it is the step that was skipped last time.

### 16.5 Bugbot — continuous, not a phase

Bugbot runs against every PR as agents work, not at phase boundaries. It owns:

- Unit and integration tests per module
- The engine's golden fixtures (§7.7) — any change that alters a fixture fails until a human confirms the change is intended
- Cross-tenant isolation tests (§13) on every PR, never only at P0
- Payment idempotency and replay tests — duplicate callbacks, double taps, concurrent attempts
- Contract tests: generated API client vs live endpoints

Bugbot findings go to the **owning** agent. It never fixes another agent's module.

**Its limits, stated so nobody relies on it wrongly:** Bugbot will not catch contradictory seed data, a wrong business rule that is consistently implemented, or two modules that each work but disagree about meaning. Those are the supervisor's job (§16.4).

### 16.6 Working rhythm

- Each agent works on its own branch, rebasing on `main` daily.
- Merges are serialised through the supervisor — never two agents merging into `main` simultaneously.
- Every agent's task brief names its owned paths, its do-not-touch list, its definition of done, and the `CONTRACT.md` sections it must read first.
- An agent that finds it needs to change another agent's file **stops and raises it**. It does not "just make it work locally".

---

## 17. Delivery phases and gates
Each phase ends with a demo and a gate — including the supervisor gate (§16.4). The next phase does not start until the gate is passed. Detailed Cursor START prompts, do-not-build lists and definitions of done per agent are written **after §18 is answered**.

| Phase | Builds | Lead agents | Gate to pass |
|---|---|---|---|
| **P0 Foundation** | Monorepo, CI, environments, tenancy + RLS, auth (OTP + portal login/2FA), RBAC, audit, outbox, OpenAPI contract pipeline, design system shell, **shared TS packages (`api-client`, `ui`, `money`, `validation`) that native will later reuse (L8)**, `CONTRACT.md` v1 | A, S | Cross-tenant isolation tests green; a portal user and a parent can log in on staging; `CONTRACT.md` ratified; supervisor gate |
| **P1 Setup, data and engine** | Institution setup screens, import wizard, canonical model, fee engine + golden fixtures, preview run, statement issue, adjustments. **No tax rules (§7.4).** | B, C, E | Pilot university's real (anonymised) data imported; preview statements match their current challans for an agreed sample; supervisor gate |
| **P2 Parent mobile web** | **Web only, no native app (L8).** Linking, home, statement with traces and diff, instalments, clearance view, deferment request, notifications (WhatsApp templates submitted), i18n Urdu | F, C | Pilot parents group can see correct statements on their phones and receive reminders — no payments yet; supervisor gate |
| **P3 Payments on mobile web** | Adapter interface; **Tier 1 adapters first** (Raast P2M, wallets), then card PSP, then consumer-number biller endpoints; receipts, polling, overpayment handling, offline payment entry, pay-on-behalf link, reconciliation | D, F | **External gate:** sandbox credentials from the pilot's bank/PSP for at least one Tier 1 channel, plus the biller spec. End-to-end paid in sandbox on **every enabled tier**; recon matches; supervisor cross-module trace (§16.4.4) |
| **P4 Native Android + iOS** | App shells consuming the same API and shared packages; native provider SDKs where they improve on the web hand-off; push, deep links, secure token storage; store submissions | G | Same journeys as P2–P3 pass on both platforms; no API or shared-package changes were needed to ship them — **if they were, L8's structuring failed and that is the finding**; supervisor gate |
| **P5 Integration and operations** | ERP connector for the pilot, outbound webhooks, clearance export, collections dashboard, defaulters, reports | C, E | ERP marks payments and clearance correctly in the pilot's test environment; supervisor gate |
| **P6 Hardening and pilot** | Load test for deadline spikes, security review, backup/restore drill, runbooks, UAT, production cut-over | All | Pilot term goes live with a rollback plan |

**Why payments precede the native apps.** The payment journey is the riskiest part of the product and the part most dependent on external parties. Proving it on mobile web first means sandbox delays block one surface instead of three, and the app stores are never in the critical path of a fix. It also tests L8 honestly: if P4 can ship without touching the API or the shared packages, the structuring worked.

**Later (post-pilot):**
- **Section 236I tax** — rules, tax lines, ATL/exemption workflow, annual tax statement (§7.4)
- **Direct bank partnerships** (§18, D14) — negotiated integrations with individual banks
- Refund tracker, SNPL partner hand-off, overseas-payer exemption workflow
- Schools mode (monthly vouchers, compliance guard)
- Option C split-settlement adapter, dedicated-database tenant tier, FBR ATL verification automation

---

## 18. Decisions to confirm before the stack freeze
Each has a recommendation. Answer (or accept) each one; the plan then moves to v1.0 and the Cursor workflows are written.

| # | Decision | Recommendation |
|---|---|---|
| D1 | Product name | Pick before P2 (app store listing, WhatsApp template approval and sender name depend on it) |
| D2 | Backend: .NET 10 + PostgreSQL vs NestJS vs Laravel | .NET 10 + PostgreSQL |
| D3 | Mobile: Expo-based React Native vs bare React Native (OBS pins) | Expo — consumer app benefits from OTA updates and simpler push/deep links |
| D4 | Parent mobile web at launch alongside the app | Yes — WhatsApp links must work without an install |
| D5 | Hosting: public cloud vs Pakistan data centre; do public universities need on-prem? | Cloud-hosted SaaS by default; ask the pilot university about data-residency requirements now |
| D6 | Tenancy: shared DB + RLS with a dedicated-DB tier later | Yes, as in §5.3 |
| D7 | Pilot university — who, and which collection bank / gateway / ERP they use | Needed before P1 ends; it decides the first adapters and connector |
| D8 | WhatsApp BSP and SMS aggregator | Choose by P2; both behind interfaces |
| D9 | Commercial model (e.g. per active student per term, or annual licence) | Decide before P4 — if usage-metered, a billing/metering module enters scope |
| D10 | Agent roster — how many Cursor agents, mapped to the roles in §16.2 | Decides how phases split. Minimum viable roster: A, B+C combined, D, E+F combined, S. The supervisor is never combined with a building agent |
| D11 | Portal language: English only at launch | Yes; Urdu for parent surfaces only |
| D12 | **Raast P2M onboarding route** — acquiring bank direct, or via a PSP? | Confirm with the pilot's collection bank at P0. This is the cheapest rail and the default channel under L7; it must not be discovered late |
| D13 | **Does the pilot's biller aggregator offer a hosted checkout** that completes without leaving our surface? | Ask at P0. If yes it moves from Tier 3 to Tier 2 (§6.3) and materially improves the fallback |
| D14 | **Direct bank partnerships** — pursue, and when? | Later phase only, and only where it beats the standard rails (§18.1) |

---

### 18.1 Direct bank partnerships (D14) — later phase, with a caveat

The intent is that a payer can be sent straight to their own bank and pay with no friction. **That is broadly what Raast P2M and the 1Link biller rail already do**, and they work with every bank on the network from day one. Signing banks individually adds value only where it buys something the rails cannot:

- A deeper embed — paying without leaving our surface at all, rather than a hand-off and return
- Pricing below the standard rail, though Raast P2M is already at or near zero
- Products the rails do not carry — fee financing, instalment credit, salary-deduction arrangements for staff children

**It does not scale the way it appears to.** Each bank is a separate commercial agreement, technical integration, certification and compliance review, with ongoing maintenance. Pakistan has roughly 30 banks, a university's parent base spans most of them, and partial coverage still requires a universal fallback — at which point the direct integrations are an optimisation, not a capability. Each one is realistically months of relationship work before any code, and a small software company sits low in a bank's integration queue.

**Sequence:** go live on Tier 1 rails → accumulate real volume across pilot institutions → approach two or three banks with that volume as leverage. Volume is what gets the meeting; a roadmap does not.

**The larger prize is probably distribution, not payments.** A bank that already banks forty universities can introduce us to all forty. That is worth more than a payment integration and is a different conversation to have with them.

---

## 19. What we need from the pilot university
1. Current fee structure documents for 2–3 programs, including scholarship, late-fee, instalment and refund policies.
2. Anonymised sample of students, enrolments and last term's challans (to validate the engine in P1).
3. Collection bank, gateway and PSP details, with contacts for sandbox access. Specifically: **the Raast P2M onboarding route (D12)**, the biller/consumer-number spec, and whether the aggregator offers a hosted checkout (D13).
4. ERP/campus system name, integration options (API, DB view, file drop) and a test environment.
5. Confirmation that they will **continue handling Section 236I in their existing process** through the pilot, since tax is deferred out of P1 (§7.4). Capture their current rates, ATL checks, exemptions and withholding statement process now, for when tax is picked up.
6. Exam clearance rule (what must be paid, by when).
7. Data-residency / hosting requirements.

---

## 20. Key risks

| Risk | Mitigation |
|---|---|
| Each bank/gateway integration is different and slow to get sandbox access | Adapter interface; request sandbox access at P0, not P3; consumer-number model as a universal fallback |
| Fee rules at a real university are messier than any template | Golden fixtures from the pilot's real challans in P1; rule parameters, not code, for variation |
| Two systems of record conflict (ERP vs platform) | Per-entity master mode (§8.2) |
| Deadline-day traffic spikes | Load test in P5; stateless API scaled horizontally; callbacks queued |
| OTP / WhatsApp costs grow with reminders | Per-tenant cost dashboard; channel preferences; rate limits |
| Tax and regulatory parameters change | All tax, late-fee and policy values are tenant configuration, never constants |
| **R8 — Tax deferral (§7.4) leaves a statutory gap** the pilot university must still meet | Confirm in writing before P1 closes that they will run 236I in their existing process during the pilot. If they cannot, tax returns to P1 scope and this plan is revised — do not let an agent improvise a partial implementation |
| **Parallel agents produce silently contradictory data or contracts** | Single-owner files, supervisor-owned `CONTRACT.md`, and a supervisor gate that must run to completion (§16). This failure mode has already occurred on a prior A2Z build |
| **Native apps require API or shared-package changes**, breaking L8's premise | Shared TS packages built in P0 and consumed by web through P2–P3; P4's gate explicitly tests for it |
| Defaulting to card payments erodes the value proposition on cost | Channel tiers ranked by cost and context loss (§6.3); Tier 1 confirmed at P0 via D12 |
| Parent trust in a new payment app | Institution branding on every screen; payment always completes on the bank's/gateway's own page; instant receipts with verification QR |
