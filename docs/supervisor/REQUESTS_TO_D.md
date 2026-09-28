# Requests to Agent D — from Agent F (Parent mobile web)

From: **Agent F — Parent mobile web** (P3 payment screens)
Date: 2026-09-28
Branch: `agent/f-p3-pay-ui`

Parent pay UI lives in `apps/parent-web/src/lib/payments/` and falls back to mock when `NEXT_PUBLIC_PARENT_DATA=mock` (default). Typed client assumes the paths below. Please confirm or correct shapes so F can drop mock.

## Endpoints you are building (assumed request/response)

| ID | Method | Path | F assumption | Screen |
|---|---|---|---|---|
| R-D-F-010 | GET | `/api/v1/payments/channels` | Query `studentId` (or tenant from session). Body `{ items: PaymentChannel[] }` with `id`, `code`, `name`, `tier` (1\|2\|3), `meta`, `handoffLabel`, `kind` (`instant`\|`wallet`\|`card_hosted`\|`voucher`) | Pay §6.4 |
| R-D-F-011 | POST | `/api/v1/payments/attempts` | Body: `studentId`, `amount` (decimal string), `selection` (`instalment`\|`full_balance`), `instalmentId?`, `channelId`. Header `Idempotency-Key`. Returns `PaymentAttempt` (§2.1 statuses). Include `redirectUrl` (system browser only) or `voucherConsumerNumber` for Tier 3. Never require card PAN. | Pay |
| R-D-F-012 | GET | `/api/v1/payments/attempts/{id}` | Same `PaymentAttempt` shape; poll-safe. Return URL is not proof — client always asks this. | Status §6.5 |
| R-D-F-013 | GET | `/api/v1/payments/receipts/{id}` | `receiptNumber`, `amount`, `paidAt`, `channelName`, `gatewayReference`, `institutionName`, `studentName`, `termLabel`, `lines[]`, `qrPayload`, `pdfUrl?` | Receipt §6.6 |
| R-D-F-014 | POST | `/api/v1/payments/pay-links` | Body: `studentId`, `amount`. Returns `{ id, url, expiresAt, preview, revoked }` where `preview` has institution, **student initials only**, term, amount, dueDate | Share §6.8 |

## Missing / needed beyond the five paths

| ID | Method | Path (proposed) | Why |
|---|---|---|---|
| R-D-F-001 | GET | `/api/v1/payments/history` | Grouped history UI (§6.6). Proposed `{ groups: [{ termLabel, items: [...] }] }` newest first. |
| R-D-F-002 | GET | `/api/v1/payments/pay-links/{token}` | Public no-account page (§6.9) needs preview without session. |
| R-D-F-003 | POST | `/api/v1/payments/pay-links/{token}/revoke` | Destructive revoke with confirm (§6.8). |
| R-D-F-004 | GET | `/api/v1/payments/receipts/{id}.pdf` | Download PDF action on receipt / success. |
| R-D-F-005 | — | Attempt fields | Please include `moneyTaken: boolean` on Failed/Expired so UI can say "No money was taken" only when true. Also `failureReason` human string. |
| R-D-F-006 | GET | Pay bootstrap (optional) | F currently composes amount options from statement mock + channels. Prefer a single `GET /api/v1/payments/pay-context?studentId=` with instalment/full amounts + channels, or document that F should keep using statement + channels. |

## Contract reminders for D

- Statuses: `Created → Pending → Succeeded | Failed | Expired | Cancelled` (+ `Reversed`). No `Paid` / `Processing` on attempts (CONTRACT §2.1 / §2.6).
- Errors: CONTRACT §3 envelope; parent renders `error.message`.
- Parent copy: **statement**, never invoice (CONTRACT §7.1).
- No card field / no WebView hand-off (plan §6.6).

## Mock demos (until wired)

- Pay: `/pay/stu-hamza`
- Status: `/payments/attempts/demo-pending` · `demo-succeeded` · `demo-failed` · `demo-expired` · `demo-voucher`
- History: `/payments/history`
- Pay-on-behalf: `/pay-on-behalf/demo-hamza`
- Share: `/pay/stu-hamza/share`
