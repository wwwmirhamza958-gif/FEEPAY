# Requests to Agent C — from Agent F (Parent mobile web)

From: **Agent F — Parent mobile web** (P2)
Date: 2026-09-28
Branch: `agent/f-p2-parent-web`

Parent web ships against **`apps/parent-web/src/lib/mock`** until these OpenAPI paths exist in `packages/api-client`. Do not invent parent EF tables in the web app.

OTP challenge/verify stubs already exist in OpenAPI (`/api/v1/auth/otp/*`). Confirm parent-facing behaviour (linked students on verify, rate-limit envelope) matches CONTRACT §3 examples.

| ID | Method | Path (proposed) | Purpose | Parent screen |
|---|---|---|---|---|
| R-C-F-001 | POST | `/api/v1/auth/otp/challenge` | Send OTP to `+92` mobile; return `challengeId` + TTL | Login (§6.1) |
| R-C-F-002 | POST | `/api/v1/auth/otp/verify` | Verify code; return session + linked student count; `NO_LINKED_STUDENTS` / `OTP_INVALID` / `OTP_RATE_LIMITED` per CONTRACT §3 | Login |
| R-C-F-003 | GET | `/api/v1/parent/me` | Parent profile (`displayName`, mobile) | Home greeting |
| R-C-F-004 | GET | `/api/v1/parent/home` | Family view: total due, next due, students **grouped by institution**, clearance alerts only when at risk | Home (§6.2) |
| R-C-F-005 | GET | `/api/v1/parent/students/{studentId}/statement` | Statement: amount due, clearance, term-over-term diff, line items with **human traces** (no rule ids), instalments; never tax or zero lines | Statement (§6.3) |
| R-C-F-006 | GET | `/api/v1/parent/students/{studentId}/statement.pdf` | Download statement PDF | Statement secondary |
| R-C-F-007 | POST | `/api/v1/parent/students/{studentId}/deferments` | Request more time (deferment); status vocabulary CONTRACT §2.5 | Statement secondary (P2 UI stub) |
| R-C-F-008 | POST | `/api/v1/parent/link-codes/redeem` | Redeem student-generated link code when mobile has no auto-linked students (§9.1 fallback) | Login / empty home |

**Money:** wire amounts as `"71250.00"` decimal strings (CONTRACT §1.4). Parent formats with `@fee/money.formatPkr`.

**Errors:** all failures use CONTRACT §3 envelope (`error.message` + `retryable`). Parent renders via `@fee/ui` `ErrorState` + Retry.

**Labels:** parent UI says **statement**, never "invoice" (CONTRACT §7.1).

**Out of scope for this request (P3):** payment attempt create/confirm, channel list, receipts, pay-on-behalf.

**Mock demos (until wired):**
- Linked mobile: `3001234567`, OTP `123456`
- Unlinked: `3000000000` → `NO_LINKED_STUDENTS`
- Rate limit: `3099999999` → `OTP_RATE_LIMITED`
- Home/statement states: `?mock=empty|error|allPaid|success`
