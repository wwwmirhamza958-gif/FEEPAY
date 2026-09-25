# Phase P1 — Supervisor gate log

| | |
|---|---|
| **Phase** | P1 Setup, data and engine |
| **Lead agents** | B (Fee engine), C (Billing & data), E (Institution portal) |
| **Contract** | `CONTRACT.md` v1.1 |
| **Status** | **In progress** — agents launched 2026-09-25 |
| **P0 note** | Staging login (R-A-011) still open; P1 proceeds on CI foundation with product-owner go-ahead |

## Ownership (do not cross)

| Agent | Owns | Never touches |
|---|---|---|
| **B** | `backend/src/Modules/FeeEngine/**`, golden fixtures | I/O, DB, screens, Billing tables |
| **C** | Academic + Billing modules, import/ERP framework, statement issue/adjustments APIs (domain) | Portal UI (`apps/institution-portal`), FeeEngine internals, Payments |
| **E** | `apps/institution-portal/**`, portal-facing API endpoints that *compose* C/B | FeeEngine internals, Billing table schemas (request C), Parent web |

## §16.4 checklist — fill when B/C/E finish

Pending. Sign-off empty until ownership audit, contract conformance, seed reconciliation, deferred-scope (§3.2 / §7.4 no tax), and Design Gate for portal screens (§11 + DESIGN §9) complete.
