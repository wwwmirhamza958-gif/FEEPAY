# Supervisor decisions — Agent B requests (P1)

Date: 2026-09-25 · Responding to B section in `REQUESTS_TO_S.md`

| ID | Decision | Action |
|---|---|---|
| B→S-1 | **Accepted** | Canonical fee-head code `LATE_FEE` added to CONTRACT §5.5 (v1.2). Seed and Billing must use this code only. |
| B→S-2 | **Deferred** | Rule JSON `schema_version = 1` property names remain owned by FeeEngine (`RuleParametersParser` + typed records). Annex in CONTRACT later if C needs a frozen wire doc; no contradictory local schemas. |

**Merge:** `agent/b-p1-fee-engine` → `main` (`407533d`). FeeEngine tests: 19 passed / 1 skipped.
