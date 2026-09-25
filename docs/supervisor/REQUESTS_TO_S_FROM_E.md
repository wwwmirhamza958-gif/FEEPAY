# Requests to Supervisor (S) — from Agent E

Date: 2026-09-25 · Branch: `agent/e-p1-portal`

| ID | Ask | Ref |
|---|---|---|
| E→S-1 | Add **Import job** statuses (CONTRACT §2.4: `Uploaded`, `Validating`, `Validated`, `FailedValidation`, `Committing`, `Succeeded`, `Failed`, `Reverted`) to `@fee/ui` `StatusPill` closed set — or confirm portal may use a shared ImportStatusPill primitive owned by A. | CONTRACT §2.4 · DESIGN §5.3 |
| E→S-2 | Add **Invoice `Draft`** to `StatusPill` kinds (preview table currently uses a local Draft chip to avoid mislabeling as Issued). | CONTRACT §2.2 |
| E→S-3 | Confirm portal dense type: body **15 px** / table row **44 px** / sidebar **240 px** as implemented matches §9 (no token fork). | DESIGN §9 |
| E→S-4 | Optional: shared `ConfirmDialog` + reason field primitive in `packages/ui` (portal has a local one for destructive financial confirms). | DESIGN §9 |

No requests to edit `DESIGN_SUPERVISOR.md` or `CONTRACT.md` token values / vocabularies — only StatusPill coverage gaps.
