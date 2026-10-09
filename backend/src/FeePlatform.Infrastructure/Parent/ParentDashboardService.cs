using FeePlatform.Infrastructure.Payments;
using FeePlatform.Infrastructure.Tenancy;
using FeePlatform.Modules.Billing;
using FeePlatform.Modules.Payments;

namespace FeePlatform.Infrastructure.Parent;

public sealed class ParentDashboardService(
    ParentAccessService access,
    PaymentService payments,
    ITenantContext tenantContext)
{
    /// <summary>Parent-facing channels only (Card, Easypaisa, JazzCash).</summary>
    public static readonly IReadOnlyList<string> ParentAdapterCodes =
    [
        AdapterCodes.EasypaisaPilot,
        AdapterCodes.JazzCashPilot,
        AdapterCodes.CardPspPilot,
    ];

    public async Task<ParentDashboardDto?> GetDashboardAsync(ParentSession session, CancellationToken ct = default)
    {
        var links = await access.ListAuthorizedStudentsAsync(session.MobileE164, ct);
        if (links.Count == 0) return null;

        // Single-student dashboard: pick the student with an open fee first, else first linked.
        AuthorizedStudentLink? chosen = null;
        Invoice? invoice = null;
        foreach (var link in links.OrderBy(l => l.DisplayName))
        {
            var open = await access.GetOpenInvoiceForStudentAsync(link.TenantId, link.StudentId, ct);
            if (open is not null)
            {
                chosen = link;
                invoice = open;
                break;
            }
        }

        chosen ??= links.OrderBy(l => l.DisplayName).First();
        invoice ??= await access.GetLatestInvoiceForStudentAsync(chosen.TenantId, chosen.StudentId, ct);

        return ToDashboard(session, chosen, invoice);
    }

    public async Task<ParentDashboardDto?> GetStudentFeeAsync(
        ParentSession session,
        Guid studentId,
        CancellationToken ct = default)
    {
        var link = await access.RequireStudentAccessAsync(session.MobileE164, studentId, ct);
        if (link is null) return null;

        var invoice = await access.GetOpenInvoiceForStudentAsync(link.TenantId, link.StudentId, ct)
            ?? await access.GetLatestInvoiceForStudentAsync(link.TenantId, link.StudentId, ct);

        return ToDashboard(session, link, invoice);
    }

    public async Task<ParentPayContextDto?> GetPayContextAsync(
        ParentSession session,
        Guid studentId,
        CancellationToken ct = default)
    {
        var link = await access.RequireStudentAccessAsync(session.MobileE164, studentId, ct);
        if (link is null) return null;

        var invoice = await access.GetOpenInvoiceForStudentAsync(link.TenantId, link.StudentId, ct);
        if (invoice is null || !InvoiceStatuses.IsPayable(invoice.Status) || invoice.RemainingBalance <= 0)
            return null;

        tenantContext.SetTenant(link.TenantId);
        var allChannels = payments.ListChannels(link.TenantId);
        var parentChannels = allChannels.AllEnabled
            .Where(c => ParentAdapterCodes.Contains(c.AdapterCode, StringComparer.OrdinalIgnoreCase))
            .OrderBy(c => c.Tier)
            .ThenBy(c => c.AdapterCode)
            .Select(c => new ParentPaymentChannelDto(
                c.AdapterCode,
                ParentChannelDisplayName(c.AdapterCode),
                c.Tier,
                ParentChannelMeta(c.AdapterCode),
                ParentChannelHandoff(c.AdapterCode)))
            .ToList();

        var dashboard = ToDashboard(session, link, invoice)!;
        return new ParentPayContextDto(
            dashboard,
            PayableTypes.Invoice,
            invoice.Id,
            parentChannels);
    }

    public async Task<PaymentAttemptDto> CreatePaymentAsync(
        ParentSession session,
        ParentCreatePaymentRequest request,
        CancellationToken ct = default)
    {
        var link = await access.RequireStudentAccessAsync(session.MobileE164, request.StudentId, ct)
            ?? throw new ParentAccessException(
                "FORBIDDEN",
                "You are not authorised to pay for this student.",
                "errors.forbidden",
                StatusCodes.Status403Forbidden);

        if (!ParentAdapterCodes.Contains(request.AdapterCode, StringComparer.OrdinalIgnoreCase))
        {
            throw new ParentAccessException(
                "VALIDATION_FAILED",
                "Choose Credit/Debit Card, Easypaisa, or JazzCash.",
                "errors.validation_failed",
                StatusCodes.Status400BadRequest);
        }

        Invoice? invoice;
        if (request.InvoiceId is { } invoiceId)
        {
            invoice = await access.RequireAuthorizedInvoiceAsync(link, invoiceId, ct);
            if (invoice is null)
            {
                throw new ParentAccessException(
                    "FORBIDDEN",
                    "That fee voucher is not available for this student.",
                    "errors.forbidden",
                    StatusCodes.Status403Forbidden);
            }
        }
        else
        {
            invoice = await access.GetOpenInvoiceForStudentAsync(link.TenantId, link.StudentId, ct);
        }

        if (invoice is null || !InvoiceStatuses.IsPayable(invoice.Status) || invoice.RemainingBalance <= 0)
        {
            throw new ParentAccessException(
                "NOTHING_DUE",
                "This student has no outstanding fee.",
                "errors.nothing_due",
                StatusCodes.Status400BadRequest);
        }

        // Set ambient tenant from authorized student — never from client header alone.
        tenantContext.SetTenant(link.TenantId);

        return await payments.CreateOrResumeAttemptAsync(
            new CreateAttemptRequest(
                PayableTypes.Invoice,
                invoice.Id,
                request.AdapterCode,
                request.IdempotencyKey),
            ct);
    }

    public async Task<IReadOnlyList<ParentPaymentHistoryItemDto>?> ListPaymentHistoryAsync(
        ParentSession session,
        Guid studentId,
        CancellationToken ct = default)
    {
        var link = await access.RequireStudentAccessAsync(session.MobileE164, studentId, ct);
        if (link is null) return null;

        tenantContext.SetTenant(link.TenantId);
        var attempts = await payments.ListAttemptsForStudentAsync(studentId, ct);

        return attempts
            .Select(a =>
            {
                var at = a.Status == PaymentAttemptStatuses.Succeeded
                    ? a.UpdatedAtUtc
                    : a.CreatedAtUtc;
                return new ParentPaymentHistoryItemDto(
                    a.AttemptId,
                    a.ReceiptId,
                    a.Amount,
                    a.Status,
                    a.AdapterCode,
                    ParentChannelDisplayName(a.AdapterCode),
                    at,
                    link.DisplayName,
                    HistoryTermLabel(at));
            })
            .ToList();
    }

    private static string HistoryTermLabel(DateTimeOffset at) =>
        at.ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

    private static ParentDashboardDto ToDashboard(
        ParentSession session,
        AuthorizedStudentLink link,
        Invoice? invoice)
    {
        var amount = invoice?.RemainingBalance ?? 0m;
        var status = invoice?.Status ?? "Issued";
        var due = invoice?.DueDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var canPay = invoice is not null
            && InvoiceStatuses.IsPayable(invoice.Status)
            && invoice.RemainingBalance > 0;

        return new ParentDashboardDto(
            session.DisplayName ?? link.GuardianDisplayName,
            session.MobileE164,
            link.StudentId,
            link.DisplayName,
            link.ExternalId,
            link.ClassLabel,
            link.TenantId,
            link.InstitutionName,
            link.TenantBrandHex,
            invoice?.Id,
            FormatMoney(amount),
            due.ToString("yyyy-MM-dd"),
            status,
            canPay,
            IsOverdue: canPay && due < DateOnly.FromDateTime(DateTime.UtcNow.Date));
    }

    private static string FormatMoney(decimal amount) => amount.ToString("0.00");

    private static string ParentChannelDisplayName(string code) => code switch
    {
        AdapterCodes.CardPspPilot => "Credit/Debit Card",
        AdapterCodes.EasypaisaPilot => "Easypaisa",
        AdapterCodes.JazzCashPilot => "JazzCash",
        _ => AdapterCodes.DisplayNameFor(code),
    };

    private static string ParentChannelMeta(string code) => code switch
    {
        AdapterCodes.CardPspPilot => "Secure card payment on the bank page",
        AdapterCodes.EasypaisaPilot => "Pay with your Easypaisa account",
        AdapterCodes.JazzCashPilot => "Pay with your JazzCash account",
        _ => string.Empty,
    };

    private static string ParentChannelHandoff(string code) => code switch
    {
        AdapterCodes.CardPspPilot => "Opening card payment…",
        AdapterCodes.EasypaisaPilot => "Opening Easypaisa…",
        AdapterCodes.JazzCashPilot => "Opening JazzCash…",
        _ => "Opening payment…",
    };
}

public sealed class ParentAccessException(
    string code,
    string message,
    string messageKey,
    int statusCode,
    bool retryable = false) : Exception(message)
{
    public string Code { get; } = code;
    public string MessageKey { get; } = messageKey;
    public int StatusCode { get; } = statusCode;
    public bool Retryable { get; } = retryable;
}

// StatusCodes constants without pulling AspNetCore into Infrastructure for this file's callers.
file static class StatusCodes
{
    public const int Status400BadRequest = 400;
    public const int Status403Forbidden = 403;
}

public sealed record ParentDashboardDto(
    string GuardianDisplayName,
    string MobileE164,
    Guid StudentId,
    string StudentName,
    string StudentExternalId,
    string ClassLabel,
    Guid TenantId,
    string InstitutionName,
    string TenantBrandHex,
    Guid? InvoiceId,
    string FeeAmount,
    string DueDate,
    string PaymentStatus,
    bool CanPay,
    bool IsOverdue);

public sealed record ParentPaymentChannelDto(
    string AdapterCode,
    string DisplayName,
    int Tier,
    string Meta,
    string HandoffLabel);

public sealed record ParentPayContextDto(
    ParentDashboardDto Fee,
    string PayableType,
    Guid PayableId,
    IReadOnlyList<ParentPaymentChannelDto> Channels);

public sealed record ParentCreatePaymentRequest(
    Guid StudentId,
    string AdapterCode,
    string IdempotencyKey,
    Guid? InvoiceId = null);

public sealed record ParentPaymentHistoryItemDto(
    Guid AttemptId,
    Guid? ReceiptId,
    string Amount,
    string Status,
    string AdapterCode,
    string ChannelName,
    DateTimeOffset PaidAtUtc,
    string StudentName,
    string TermLabel);
