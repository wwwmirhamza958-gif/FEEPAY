# FeeEngine module

Pure calculation library for Fee Platform (plan §7). **No database, HTTP, or side effects.**

Owned by **Agent B**. Agent **C (Billing)** calls this library to preview and issue statements.

## Public API

```csharp
using FeePlatform.Modules.FeeEngine.Api;

IFeeEngine engine = new FeeEngine();
FeeEngineResult result = engine.Calculate(input);
```

| Type | Role |
|---|---|
| `IFeeEngine` / `FeeEngine` | Entry point |
| `FeeEngineInput` | Enrolment, heads, rules, discounts, optional late fee / instalments / adjustments / prior snapshot |
| `FeeEngineResult` | Statement lines (each with `Trace`), totals, optional instalments + term diff |
| `FeeEngineResult.TaxLines` | Always empty in P1 (plan §7.4) |

Preview mode: set `FeeEngineInput.IsPreview = true` (default). The engine is already pure — Billing chooses whether to persist.

## Rule types (schema_version = 1)

| Type | Parameters |
|---|---|
| `PerCreditHour` | `defaultRate`, optional `categoryRates`, optional `includedHours` + `overloadRate` |
| `PerCourse` | `amountPerCourse` |
| `PerRepeatCourse` | `amountPerCourse` |
| `FlatPerTerm` | `amount` |
| `FlatMonthly` | `amountPerMonth` (× enrolment `MonthCount`) |
| `OneTime` | `amount`, `firstTermOnly` |
| `Conditional` | `amount`, `attributeKey`, optional `expectedValue` |

`TaxRuleDefinition` may be passed on the input; **nothing is emitted** for tax.

## Helpers C may also call

- `DiscountStacking.Apply(...)` — order + max caps
- `InstalmentSplitter.Split(netTotal, plan)` — 50/50, 40/30/30, …
- `LateFeeCalculator.Calculate(request)` — fixed / per-day / per-week (+ optional cap)
- `TermDiffCalculator.Diff(previousSnapshot, currentLines)`
- `MoneyMath.RoundHalfUp` / `FormatPkr` / `ToWire` — CONTRACT §1
- `FeeEngineJson.SerializeResult` — golden / wire DTO

## Money

- `decimal` only; round **per line** half-up (`MidpointRounding.AwayFromZero`)
- Traces use parent display form (`Rs 142,500`, `−Rs 35,625` with U+2009 / U+2212)

## Golden fixtures

Committed under `backend/tests/FeePlatform.FeeEngine.Tests/Fixtures/*.expected.json`. Every fixture must stay green.
