using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Modules.Billing;
using FeePlatform.Modules.Payments;
using Microsoft.EntityFrameworkCore;

namespace FeePlatform.Infrastructure.Payments;

/// <summary>
/// Provisional applier owned by Infrastructure for P3 until Agent C exposes a first-class
/// BillingService.ApplyPaidAmount (see REQUESTS_TO_C_FROM_D.md). Does not edit FeeEngine.
/// </summary>
public sealed class BillingInvoicePaymentApplier(AppDbContext db) : IInvoicePaymentApplier
{
    public async Task<PaymentApplicationResult> ApplyPaidAmountAsync(
        PaymentApplicationRequest request,
        CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Instalments)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new PaymentsDomainException(
                "RESOURCE_NOT_FOUND",
                "We couldn't find that record.",
                "errors.resource_not_found");

        if (invoice.StudentId != request.StudentId)
        {
            throw new PaymentsDomainException(
                "VALIDATION_FAILED",
                "Student does not match invoice.",
                "errors.validation_failed");
        }

        var remaining = invoice.RemainingBalance;
        var apply = Math.Min(request.Amount, Math.Max(0, remaining));
        var overpay = request.Amount - apply;

        if (request.InstalmentId is Guid instalmentId)
        {
            var instalment = invoice.Instalments.FirstOrDefault(i => i.Id == instalmentId);
            if (instalment is not null)
            {
                var instalmentDue = instalment.Amount - instalment.PaidAmount;
                var toInstalment = Math.Min(apply, Math.Max(0, instalmentDue));
                instalment.PaidAmount += toInstalment;
            }
        }
        else if (invoice.Instalments.Count > 0 && apply > 0)
        {
            var left = apply;
            foreach (var instalment in invoice.Instalments.OrderBy(i => i.Sequence))
            {
                var due = instalment.Amount - instalment.PaidAmount;
                if (due <= 0) continue;
                var chunk = Math.Min(left, due);
                instalment.PaidAmount += chunk;
                left -= chunk;
                if (left <= 0) break;
            }
        }

        invoice.RemainingBalance = BillingWire.RoundHalfUp(invoice.RemainingBalance - apply);
        if (invoice.RemainingBalance < 0)
            invoice.RemainingBalance = 0;

        if (invoice.RemainingBalance == 0 && InvoiceStatuses.IsPayable(invoice.Status))
            invoice.Status = InvoiceStatuses.Paid;
        else if (apply > 0 && invoice.Status == InvoiceStatuses.Issued)
            invoice.Status = InvoiceStatuses.PartiallyPaid;

        Guid? creditId = null;
        if (overpay > 0 && request.AllowOverpaymentCredit)
        {
            creditId = Guid.NewGuid();
            db.StudentCredits.Add(new StudentCredit
            {
                Id = creditId.Value,
                TenantId = request.TenantId,
                StudentId = request.StudentId,
                Amount = BillingWire.RoundHalfUp(overpay),
                Currency = "PKR",
                SourceNote = "Overpayment from payment attempt",
                SourceAttemptId = request.SourceAttemptId,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
        }
        else if (overpay > 0)
        {
            // Never silently drop — flag via credit even if caller forgot the flag.
            creditId = Guid.NewGuid();
            db.StudentCredits.Add(new StudentCredit
            {
                Id = creditId.Value,
                TenantId = request.TenantId,
                StudentId = request.StudentId,
                Amount = BillingWire.RoundHalfUp(overpay),
                Currency = "PKR",
                SourceNote = "Overpayment (forced credit — not dropped)",
                SourceAttemptId = request.SourceAttemptId,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        // Caller owns SaveChanges / outer transaction.
        return new PaymentApplicationResult(
            apply,
            overpay > 0 ? BillingWire.RoundHalfUp(overpay) : 0,
            creditId,
            invoice.Status,
            invoice.RemainingBalance);
    }
}

public sealed class BillingPayableLookup(AppDbContext db) : IPayableLookup
{
    public async Task<PayableSnapshot?> GetPayableAsync(
        string payableType,
        Guid payableId,
        CancellationToken ct = default)
    {
        if (string.Equals(payableType, PayableTypes.Instalment, StringComparison.OrdinalIgnoreCase))
        {
            var instalment = await db.Instalments.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == payableId, ct);
            if (instalment is null) return null;

            var invoice = await db.Invoices.AsNoTracking()
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == instalment.InvoiceId, ct);
            if (invoice is null) return null;

            var student = await db.Students.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == invoice.StudentId, ct);

            var due = instalment.Amount - instalment.PaidAmount;
            return new PayableSnapshot(
                instalment.Id,
                PayableTypes.Instalment,
                invoice.Id,
                invoice.StudentId,
                invoice.Version,
                due,
                invoice.Status,
                invoice.Lines.OrderBy(l => l.SortOrder)
                    .Select(l => new PayableLineSnapshot(l.FeeHeadCode, l.Amount, l.Trace))
                    .ToList(),
                student?.DisplayName);
        }

        // Default: Invoice
        var inv = await db.Invoices.AsNoTracking()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == payableId, ct);
        if (inv is null) return null;

        var studentName = await db.Students.AsNoTracking()
            .Where(s => s.Id == inv.StudentId)
            .Select(s => s.DisplayName)
            .FirstOrDefaultAsync(ct);

        return new PayableSnapshot(
            inv.Id,
            PayableTypes.Invoice,
            inv.Id,
            inv.StudentId,
            inv.Version,
            inv.RemainingBalance,
            inv.Status,
            inv.Lines.OrderBy(l => l.SortOrder)
                .Select(l => new PayableLineSnapshot(l.FeeHeadCode, l.Amount, l.Trace))
                .ToList(),
            studentName);
    }
}
