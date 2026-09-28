# Requests to Agent C (Billing) from Agent D (Payments) — P3

Date: 2026-09-28

## D→C-1 — Apply paid amount on verified success

**Need:** First-class Billing API to apply a verified payment to an invoice / instalment and optionally credit overpayment to `StudentCredit`, in the caller's DbContext transaction.

**Proposed signature** (Payments already owns this contract as `IInvoicePaymentApplier`):

```csharp
Task<PaymentApplicationResult> ApplyPaidAmountAsync(PaymentApplicationRequest request, CancellationToken ct);
```

Where `PaymentApplicationRequest` includes `InvoiceId`, optional `InstalmentId`, `StudentId`, `Amount`, `SourceAttemptId`, `AllowOverpaymentCredit`.

**Behaviour:**

- Reduce `Invoice.RemainingBalance` / instalment `PaidAmount`.
- Transition `Issued` → `PartiallyPaid` / `Paid` per CONTRACT §2.2.
- Overpayment → `StudentCredit` + outbox `OverpaymentCredited` (Payments currently emits the outbox event; Billing may own it later).
- Never silently drop overpayment.

**Interim:** Infrastructure `BillingInvoicePaymentApplier` implements the interface against existing Billing entities without editing FeeEngine. Please replace with `BillingService` method and have DI point to it when ready.

## D→C-2 — Payable snapshot for attempt creation

**Need:** Stable way to read payable amount, invoice version, student, and lines for attempt snapshots (CONTRACT §1.5).

**Interim:** `IPayableLookup` / `BillingPayableLookup` reads invoices/instalments. Prefer a Billing-owned `GetPayableForPaymentAsync` if C wants to hide entity shapes.
