# Supervisor decisions — Agent C requests (P1)

Date: 2026-09-25 · From C section in `REQUESTS_TO_S.md`

| ID | Decision | Action |
|---|---|---|
| C→S-1 | **Accepted** | `Enrolled` / `Dropped` are **academic enrolment** lifecycle words, not Invoice/PaymentAttempt statuses. Keep them off CONTRACT §2. Document in Academic module README only. |
| C→S-2 | **Accepted for P1** | `X-Tenant-Id` header stub OK until session→tenant lands. **R-A-016** for A. |
| C→S-3 | **Accepted** | A's seed runner must invoke `AcademicPilotSeed` (and FeeEngine does not own seed). **R-A-017**. |
| C→S-4 | **Accepted for P1** | Fee-schedule / discount **writes** may 501. Portal Design Gate: show honest empty/disabled write UX, not silent failure. Mutable schedule ownership stays with C (+ engine params from B); not a B UI concern. |
| C→S-5 | **Noted** | `LATE_FEE` + real `IFeeEngine` wiring confirmed. |

**Merge:** `agent/c-p1-billing-data` → `main` after tests green.
