# Requests to Agent B (Fee engine) — from Agent C

| ID | Status | Request | Notes |
|---|---|---|---|
| C→B-1 | **Done** | Public `IFeeEngine` | Wired to `FeePlatform.Modules.FeeEngine.Api.IFeeEngine` / `FeeEngine` (B merged). |
| C→B-2 | **Done** | Half-up per line | Billing uses `FeeEngine.Money.MoneyMath`. |
| C→B-3 | **Done** | DI registration | Infrastructure registers `IFeeEngine` → `FeeEngine`. |
| C→B-4 | **Done** | Fee head codes §5.5 incl. `LATE_FEE` | Catalog + engine input include `LATE_FEE`; no tax lines persisted. |

Local `StubFeeEngine` removed after B merge.
