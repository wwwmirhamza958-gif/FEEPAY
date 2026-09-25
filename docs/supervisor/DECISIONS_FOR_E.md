# Supervisor decisions — Agent E requests (P1)

Date: 2026-09-25 · Responding to `REQUESTS_TO_S_FROM_E.md`

| ID | Decision | Action |
|---|---|---|
| E→S-1 | **Accepted** | Import job statuses (§2.4) belong on shared `StatusPill` (or a thin `ImportStatusPill` re-exporting the same tones). **A** implements — see R-A-013. Portal must not keep a private lookalike. |
| E→S-2 | **Accepted** | Invoice `Draft` is a CONTRACT §2.2 status — add to `StatusPillKind`. **A** implements — R-A-014. Local Draft chip may remain until A merges. |
| E→S-3 | **Confirmed** | Body 15 px / row 44 px / sidebar 240 px matches DESIGN §9. No token fork. |
| E→S-4 | **Accepted** | Shared `ConfirmDialog` (reason field optional prop) in `packages/ui`. **A** implements — R-A-015. E replaces local copy after. |

Design Gate (spot-check on `agent/e-p1-portal`): no raw hex / `transition: all` / physical `ml|mr` in portal feature files; setup/import/billing shells present with confirm+reason on destructive actions. Full §11 matrix deferred until wired to live C APIs (currently mocks).

**Merge:** E portal commit eligible for `main` independently of B/C.
