using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FeePlatform.Infrastructure.Outbox;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Infrastructure.Tenancy;
using FeePlatform.Modules.IdentityTenancy;
using FeePlatform.Modules.IdentityTenancy.Seed;
using FeePlatform.Modules.Payments;
using FeePlatform.Modules.Payments.Receipts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FeePlatform.Infrastructure.Payments;

public sealed class PaymentService(
    AppDbContext db,
    ITenantContext tenantContext,
    IEnumerable<IPaymentAdapter> adapters,
    IInvoicePaymentApplier paymentApplier,
    IPayableLookup payableLookup,
    IOptions<PaymentsOptions> options,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly Dictionary<string, IPaymentAdapter> _adapters =
        adapters.ToDictionary(a => a.Code, StringComparer.OrdinalIgnoreCase);

    public PaymentChannelsResponse ListChannels(Guid? tenantIdQuery)
    {
        var tenantId = RequireTenant(tenantIdQuery);
        EnsureAdapterConfigs(tenantId);

        var enabled = db.TenantAdapterConfigs.AsNoTracking()
            .Where(c => c.Enabled)
            .AsEnumerable()
            .Select(c => new PaymentChannelDto(
                c.AdapterCode,
                AdapterCodes.DisplayNameFor(c.AdapterCode),
                c.Tier,
                true,
                IsDefault: false))
            .OrderBy(c => c.Tier)
            .ThenBy(c => c.AdapterCode)
            .ToList();

        if (enabled.Count == 0)
        {
            // Default: all CONTRACT §5.2 codes enabled for pilot until tenant config seeded.
            enabled = AdapterCodes.All.Select(code => new PaymentChannelDto(
                code,
                AdapterCodes.DisplayNameFor(code),
                AdapterCodes.TierFor(code),
                true,
                false)).OrderBy(c => c.Tier).ToList();
        }

        var highest = enabled.Min(c => c.Tier);
        var defaultChannel = enabled.First(c => c.Tier == highest) with { IsDefault = true };
        var others = enabled.Where(c => c.AdapterCode != defaultChannel.AdapterCode)
            .Select(c => c with { IsDefault = false })
            .ToList();

        // Do not flatten — return highest tier as default + others separately (plan §6.3).
        return new PaymentChannelsResponse(
            highest,
            defaultChannel,
            others,
            enabled.Select(c => c with { IsDefault = c.AdapterCode == defaultChannel.AdapterCode }).ToList());
    }

    public async Task<PaymentAttemptDto> CreateOrResumeAttemptAsync(CreateAttemptRequest request, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new PaymentsDomainException(
                "VALIDATION_FAILED",
                "idempotencyKey is required.",
                "errors.validation_failed");
        }

        if (string.IsNullOrWhiteSpace(request.AdapterCode) || !_adapters.ContainsKey(request.AdapterCode))
        {
            throw new PaymentsDomainException(
                "VALIDATION_FAILED",
                "Unknown adapterCode.",
                "errors.validation_failed");
        }

        var existingByKey = await db.PaymentAttempts
            .FirstOrDefaultAsync(a => a.IdempotencyKey == request.IdempotencyKey, ct);
        if (existingByKey is not null)
            return await EnsureCheckoutAsync(existingByKey, ct);

        var active = await db.PaymentAttempts
            .Where(a => a.PayableType == request.PayableType
                        && a.PayableId == request.PayableId
                        && (a.Status == PaymentAttemptStatuses.Created || a.Status == PaymentAttemptStatuses.Pending))
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (active is not null)
            return await EnsureCheckoutAsync(active, ct);

        var payable = await payableLookup.GetPayableAsync(request.PayableType, request.PayableId, ct)
            ?? throw new PaymentsDomainException(
                "RESOURCE_NOT_FOUND",
                "We couldn't find that record.",
                "errors.resource_not_found");

        if (payable.AmountDue <= 0)
        {
            throw new PaymentsDomainException(
                "NOTHING_DUE",
                "This item has no remaining balance.",
                "errors.nothing_due");
        }

        var now = DateTimeOffset.UtcNow;
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayableType = payable.PayableType,
            PayableId = payable.PayableId,
            InvoiceId = payable.InvoiceId,
            StudentId = payable.StudentId,
            InvoiceVersion = payable.InvoiceVersion,
            Amount = payable.AmountDue,
            Currency = "PKR",
            Status = PaymentAttemptStatuses.Created,
            AdapterCode = request.AdapterCode,
            IdempotencyKey = request.IdempotencyKey.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ExpiresAtUtc = now.AddHours(2),
        };

        db.PaymentAttempts.Add(attempt);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventName = DomainEvents.PaymentAttemptCreated,
            OccurredAtUtc = now,
            PayloadJson = JsonSerializer.Serialize(new
            {
                tenantId,
                attemptId = attempt.Id,
                payableType = attempt.PayableType,
                payableId = attempt.PayableId,
                amount = PaymentsWire.ToWire(attempt.Amount),
                idempotencyKey = attempt.IdempotencyKey,
                occurredAt = now,
            }, JsonOptions),
        });

        await db.SaveChangesAsync(ct);
        return await EnsureCheckoutAsync(attempt, ct);
    }

    public async Task<PaymentAttemptDto> GetAttemptAsync(Guid attemptId, CancellationToken ct = default)
    {
        // Allow lookup without ambient tenant when caller already authorized (parent session path).
        var attempt = await db.PaymentAttempts.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw new PaymentsDomainException(
                "RESOURCE_NOT_FOUND",
                "We couldn't find that record.",
                "errors.resource_not_found");

        if (tenantContext.TenantId is Guid tid && tid != attempt.TenantId && !tenantContext.IsPlatformAdminBypass)
        {
            throw new PaymentsDomainException(
                "RESOURCE_NOT_FOUND",
                "We couldn't find that record.",
                "errors.resource_not_found");
        }

        if (tenantContext.TenantId is null)
            tenantContext.SetTenant(attempt.TenantId);

        return ToDto(attempt);
    }

    /// <summary>Parent / finance history — attempts for one student (caller must authorize).</summary>
    public async Task<IReadOnlyList<PaymentAttemptDto>> ListAttemptsForStudentAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        var query = db.PaymentAttempts.IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.StudentId == studentId);

        if (tenantContext.TenantId is Guid tid && !tenantContext.IsPlatformAdminBypass)
            query = query.Where(a => a.TenantId == tid);

        var attempts = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(100)
            .ToListAsync(ct);

        return attempts.Select(ToDto).ToList();
    }

    public async Task<PaymentAttemptDto> HandleCallbackAsync(
        string adapterCode,
        IReadOnlyDictionary<string, string> headers,
        string rawBody,
        CancellationToken ct = default)
    {
        if (!_adapters.TryGetValue(adapterCode, out var adapter))
        {
            throw new PaymentsDomainException(
                "VALIDATION_FAILED",
                "Unknown adapterCode.",
                "errors.validation_failed");
        }

        var parsed = await adapter.VerifyAndParseCallbackAsync(
            new AdapterCallbackRequest(adapterCode, headers, rawBody), ct);

        if (!parsed.SignatureValid)
        {
            throw new PaymentsDomainException(
                "CALLBACK_SIGNATURE_INVALID",
                "Payment callback signature verification failed.",
                "errors.callback_signature_invalid");
        }

        // Callbacks may arrive without tenant header — resolve from attempt.
        PaymentAttempt? attempt = null;
        if (parsed.AttemptId is Guid aid)
        {
            attempt = await db.PaymentAttempts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == aid, ct);
        }
        else if (!string.IsNullOrWhiteSpace(parsed.GatewayRef))
        {
            attempt = await db.PaymentAttempts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.GatewayRef == parsed.GatewayRef && a.AdapterCode == adapterCode, ct);
        }

        if (attempt is null)
        {
            throw new PaymentsDomainException(
                "RESOURCE_NOT_FOUND",
                "We couldn't find that record.",
                "errors.resource_not_found");
        }

        tenantContext.SetTenant(attempt.TenantId);

        if (attempt.Status == PaymentAttemptStatuses.Succeeded)
            return ToDto(attempt); // duplicate callback — harmless

        if (!string.IsNullOrWhiteSpace(parsed.GatewayTxnId))
        {
            var dup = await db.PaymentAttempts.IgnoreQueryFilters()
                .AnyAsync(a => a.AdapterCode == adapterCode
                               && a.GatewayTxnId == parsed.GatewayTxnId
                               && a.Id != attempt.Id, ct);
            if (dup)
            {
                // Unique (adapter, gateway_txn_id) — treat as idempotent replay if same logical txn already stored.
                var existing = await db.PaymentAttempts.IgnoreQueryFilters()
                    .FirstAsync(a => a.AdapterCode == adapterCode && a.GatewayTxnId == parsed.GatewayTxnId, ct);
                return ToDto(existing);
            }
        }

        if (parsed.Status == AdapterPaymentStatus.Failed
            || parsed.Status == AdapterPaymentStatus.Expired
            || parsed.Status == AdapterPaymentStatus.Cancelled)
        {
            attempt.Status = parsed.Status switch
            {
                AdapterPaymentStatus.Expired => PaymentAttemptStatuses.Expired,
                AdapterPaymentStatus.Cancelled => PaymentAttemptStatuses.Cancelled,
                _ => PaymentAttemptStatuses.Failed,
            };
            attempt.FailureReasonCode = parsed.ReasonCode;
            attempt.GatewayTxnId ??= parsed.GatewayTxnId;
            attempt.UpdatedAtUtc = DateTimeOffset.UtcNow;

            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                TenantId = attempt.TenantId,
                EventName = DomainEvents.PaymentFailed,
                OccurredAtUtc = attempt.UpdatedAtUtc,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    tenantId = attempt.TenantId,
                    attemptId = attempt.Id,
                    reasonCode = attempt.FailureReasonCode ?? "FAILED",
                    occurredAt = attempt.UpdatedAtUtc,
                }, JsonOptions),
            });

            await db.SaveChangesAsync(ct);
            return ToDto(attempt);
        }

        if (parsed.Status != AdapterPaymentStatus.Succeeded)
        {
            attempt.Status = PaymentAttemptStatuses.Pending;
            attempt.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return ToDto(attempt);
        }

        return await CompleteSuccessAsync(attempt, parsed.GatewayTxnId, ct);
    }

    public async Task<PaymentReceiptDto> GetReceiptAsync(Guid receiptId, bool includePdf, CancellationToken ct = default)
    {
        RequireTenant();
        var receipt = await db.PaymentReceipts.FirstOrDefaultAsync(r => r.Id == receiptId, ct)
            ?? throw new PaymentsDomainException(
                "RESOURCE_NOT_FOUND",
                "We couldn't find that record.",
                "errors.resource_not_found");

        if (includePdf && (receipt.PdfBytes is null || receipt.PdfBytes.Length == 0))
        {
            receipt.PdfBytes = BuildPdf(receipt);
            await db.SaveChangesAsync(ct);
        }

        return ToReceiptDto(receipt);
    }

    public async Task<byte[]> GetReceiptPdfAsync(Guid receiptId, CancellationToken ct = default)
    {
        RequireTenant();
        var receipt = await db.PaymentReceipts.FirstOrDefaultAsync(r => r.Id == receiptId, ct)
            ?? throw new PaymentsDomainException(
                "RESOURCE_NOT_FOUND",
                "We couldn't find that record.",
                "errors.resource_not_found");

        if (receipt.PdfBytes is null || receipt.PdfBytes.Length == 0)
        {
            receipt.PdfBytes = BuildPdf(receipt);
            await db.SaveChangesAsync(ct);
        }

        return receipt.PdfBytes!;
    }

    public async Task<ReceiptVerifyDto> VerifyReceiptAsync(string token, CancellationToken ct = default)
    {
        tenantContext.SetPlatformAdminBypass(true);
        try
        {
            var receipt = await db.PaymentReceipts.AsNoTracking()
                .FirstOrDefaultAsync(r => r.VerifyToken == token, ct);
            if (receipt is null)
            {
                return new ReceiptVerifyDto(false, "0.00", DateOnly.FromDateTime(DateTime.UtcNow), "—", "", null);
            }

            return new ReceiptVerifyDto(
                true,
                PaymentsWire.ToWire(receipt.Amount),
                DateOnly.FromDateTime(receipt.CreatedAtUtc.UtcDateTime),
                receipt.StudentInitials,
                receipt.InstitutionName,
                receipt.ReceiptNumber);
        }
        finally
        {
            tenantContext.SetPlatformAdminBypass(false);
        }
    }

    public async Task<PayLinkDto> CreatePayLinkAsync(CreatePayLinkRequest request, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var payable = await payableLookup.GetPayableAsync(PayableTypes.Invoice, request.InvoiceId, ct)
            ?? throw new PaymentsDomainException(
                "RESOURCE_NOT_FOUND",
                "We couldn't find that record.",
                "errors.resource_not_found");

        var hours = request.ExpiresInHours <= 0 ? 72 : request.ExpiresInHours;
        var expires = DateTimeOffset.UtcNow.AddHours(hours);
        var token = CreateSignedPayLinkToken(tenantId, request.InvoiceId, expires);

        var link = new PayLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = request.InvoiceId,
            Token = token,
            ExpiresAtUtc = expires,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = request.CreatedByUserId,
        };
        db.PayLinks.Add(link);
        await db.SaveChangesAsync(ct);

        var url = $"{options.Value.PublicBaseUrl.TrimEnd('/')}/pay/{token}";
        return new PayLinkDto(link.Id, payable.InvoiceId, token, url, expires);
    }

    public async Task<PaymentAttemptDto> RecordOfflinePaymentAsync(
        CreateOfflinePaymentRequest request,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (string.IsNullOrWhiteSpace(request.Reference))
        {
            throw new PaymentsDomainException(
                "VALIDATION_FAILED",
                "Reference is required for offline payments.",
                "errors.validation_failed");
        }

        if (!PaymentsWire.TryParseWire(request.Amount, out var amount) || amount <= 0)
        {
            throw new PaymentsDomainException(
                "INVALID_AMOUNT",
                "Amount must be a decimal string with two places.",
                "errors.invalid_amount");
        }

        var payable = await payableLookup.GetPayableAsync(PayableTypes.Invoice, request.InvoiceId, ct)
            ?? throw new PaymentsDomainException(
                "RESOURCE_NOT_FOUND",
                "We couldn't find that record.",
                "errors.resource_not_found");

        var now = DateTimeOffset.UtcNow;
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayableType = PayableTypes.Invoice,
            PayableId = request.InvoiceId,
            InvoiceId = request.InvoiceId,
            StudentId = payable.StudentId,
            InvoiceVersion = payable.InvoiceVersion,
            Amount = amount,
            Currency = "PKR",
            Status = PaymentAttemptStatuses.Pending,
            AdapterCode = "offline-manual",
            IdempotencyKey = $"offline:{request.Reference.Trim()}:{request.InvoiceId:N}",
            GatewayTxnId = request.Reference.Trim(),
            GatewayRef = request.Reference.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        var existing = await db.PaymentAttempts
            .FirstOrDefaultAsync(a => a.IdempotencyKey == attempt.IdempotencyKey, ct);
        if (existing is not null)
            return ToDto(existing);

        db.PaymentAttempts.Add(attempt);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventName = DomainEvents.OfflinePaymentRecorded,
            OccurredAtUtc = now,
            PayloadJson = JsonSerializer.Serialize(new
            {
                tenantId,
                paymentId = attempt.Id,
                studentId = attempt.StudentId,
                amount = PaymentsWire.ToWire(amount),
                reference = request.Reference.Trim(),
                recordedByUserId = request.RecordedByUserId,
                occurredAt = now,
            }, JsonOptions),
        });

        await db.SaveChangesAsync(ct);
        return await CompleteSuccessAsync(attempt, request.Reference.Trim(), ct);
    }

    public async Task<ReconciliationRunDto> RunReconciliationStubAsync(
        string adapterCode,
        DateOnly reportDate,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (!_adapters.TryGetValue(adapterCode, out var adapter))
        {
            throw new PaymentsDomainException(
                "VALIDATION_FAILED",
                "Unknown adapterCode.",
                "errors.validation_failed");
        }

        IReadOnlyList<SettlementLine> settlement;
        try
        {
            settlement = await adapter.FetchSettlementReportAsync(reportDate, ct);
        }
        catch (PaymentsDomainException)
        {
            settlement = [];
        }

        var platformSucceeded = await db.PaymentAttempts.AsNoTracking()
            .Where(a => a.AdapterCode == adapterCode
                        && a.Status == PaymentAttemptStatuses.Succeeded
                        && a.UpdatedAtUtc >= reportDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                        && a.UpdatedAtUtc < reportDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .ToListAsync(ct);

        var run = new ReconciliationRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AdapterCode = adapterCode,
            ReportDate = reportDate,
            StartedAtUtc = DateTimeOffset.UtcNow,
        };

        var settlementByTxn = settlement
            .Where(s => !string.IsNullOrWhiteSpace(s.GatewayTxnId))
            .GroupBy(s => s.GatewayTxnId)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var platformByTxn = platformSucceeded
            .Where(a => !string.IsNullOrWhiteSpace(a.GatewayTxnId))
            .ToDictionary(a => a.GatewayTxnId!, a => a, StringComparer.Ordinal);

        foreach (var line in settlementByTxn.Values)
        {
            if (!platformByTxn.TryGetValue(line.GatewayTxnId, out var attempt))
            {
                run.Exceptions.Add(new ReconciliationException
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ReconciliationRunId = run.Id,
                    ExceptionType = ReconciliationExceptionTypes.PaidAtGatewayNotInPlatform,
                    GatewayTxnId = line.GatewayTxnId,
                    GatewayAmount = line.Amount,
                    Detail = "Paid at gateway but not in platform.",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                });
            }
            else if (attempt.Amount != line.Amount)
            {
                run.Exceptions.Add(new ReconciliationException
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ReconciliationRunId = run.Id,
                    ExceptionType = ReconciliationExceptionTypes.AmountMismatch,
                    GatewayTxnId = line.GatewayTxnId,
                    PaymentAttemptId = attempt.Id,
                    GatewayAmount = line.Amount,
                    PlatformAmount = attempt.Amount,
                    Detail = "Gateway amount does not match platform attempt amount.",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                });
            }
        }

        foreach (var attempt in platformByTxn.Values)
        {
            if (!settlementByTxn.ContainsKey(attempt.GatewayTxnId!))
            {
                run.Exceptions.Add(new ReconciliationException
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ReconciliationRunId = run.Id,
                    ExceptionType = ReconciliationExceptionTypes.InPlatformNotSettled,
                    GatewayTxnId = attempt.GatewayTxnId,
                    PaymentAttemptId = attempt.Id,
                    PlatformAmount = attempt.Amount,
                    Detail = "In platform but not settled on gateway report.",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                });
            }
        }

        // Always surface the three exception type slots in stub documentation when report empty.
        if (run.Exceptions.Count == 0 && settlement.Count == 0)
        {
            // Stub run with zero exceptions is valid — types are defined for when lines exist.
        }

        run.CompletedAtUtc = DateTimeOffset.UtcNow;
        db.ReconciliationRuns.Add(run);
        await db.SaveChangesAsync(ct);

        return new ReconciliationRunDto(
            run.Id,
            run.AdapterCode,
            run.ReportDate,
            run.StartedAtUtc,
            run.CompletedAtUtc,
            run.Exceptions.Select(e => new ReconciliationExceptionDto(
                e.Id,
                e.ExceptionType,
                e.GatewayTxnId,
                e.PaymentAttemptId,
                e.GatewayAmount is null ? null : PaymentsWire.ToWire(e.GatewayAmount.Value),
                e.PlatformAmount is null ? null : PaymentsWire.ToWire(e.PlatformAmount.Value),
                e.Detail)).ToList());
    }

    public async Task<PaymentAttemptDto> CompleteSandboxFakeAsync(Guid attemptId, CancellationToken ct = default)
    {
        if (!(options.Value.AllowSandboxFake && environment.IsDevelopment()))
        {
            throw new PaymentsDomainException(
                "PAYMENTS_SANDBOX_NOT_CONFIGURED",
                "FakeTier1 is only available when Payments:AllowSandboxFake=true in Development.",
                "errors.payments_sandbox_not_configured");
        }

        var attempt = await db.PaymentAttempts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw new PaymentsDomainException(
                "RESOURCE_NOT_FOUND",
                "We couldn't find that record.",
                "errors.resource_not_found");

        tenantContext.SetTenant(attempt.TenantId);

        if (attempt.Status == PaymentAttemptStatuses.Succeeded)
            return ToDto(attempt);

        if (!AdapterCodes.IsTier1(attempt.AdapterCode))
        {
            throw new PaymentsDomainException(
                "VALIDATION_FAILED",
                "FakeTier1 only applies to Tier 1 adapters.",
                "errors.validation_failed");
        }

        return await CompleteSuccessAsync(attempt, $"fake-txn-{attempt.Id:N}", ct);
    }

    private async Task<PaymentAttemptDto> EnsureCheckoutAsync(PaymentAttempt attempt, CancellationToken ct)
    {
        if (attempt.Status is PaymentAttemptStatuses.Succeeded or PaymentAttemptStatuses.Failed
            or PaymentAttemptStatuses.Expired or PaymentAttemptStatuses.Cancelled or PaymentAttemptStatuses.Reversed)
            return ToDto(attempt);

        if (!string.IsNullOrWhiteSpace(attempt.CheckoutUrl) && attempt.Status == PaymentAttemptStatuses.Pending)
            return ToDto(attempt);

        if (!_adapters.TryGetValue(attempt.AdapterCode, out var adapter))
        {
            throw new PaymentsDomainException(
                "VALIDATION_FAILED",
                "Unknown adapterCode.",
                "errors.validation_failed");
        }

        var baseUrl = options.Value.PublicBaseUrl.TrimEnd('/');
        var checkout = await adapter.CreateCheckoutAsync(new AdapterCheckoutRequest(
            attempt.Id,
            attempt.TenantId,
            attempt.Amount,
            attempt.Currency,
            attempt.IdempotencyKey,
            ReturnUrl: $"{baseUrl}/payments/return/{attempt.Id:D}",
            CallbackUrl: $"{baseUrl}/api/v1/payments/callbacks/{attempt.AdapterCode}"), ct);

        attempt.CheckoutUrl = checkout.RedirectUrl;
        attempt.GatewayRef = checkout.GatewayRef;
        attempt.Status = PaymentAttemptStatuses.Pending;
        attempt.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(attempt);
    }

    private async Task<PaymentAttemptDto> CompleteSuccessAsync(
        PaymentAttempt attempt,
        string? gatewayTxnId,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        // Relational providers get a real transaction; EF InMemory (tests) does not support one.
        var useTx = db.Database.IsRelational();
        await using var tx = useTx ? await db.Database.BeginTransactionAsync(ct) : null;

        attempt.GatewayTxnId = gatewayTxnId ?? attempt.GatewayTxnId ?? $"txn-{attempt.Id:N}";
        attempt.Status = PaymentAttemptStatuses.Succeeded;
        attempt.UpdatedAtUtc = now;

        Guid? instalmentId = attempt.PayableType == PayableTypes.Instalment ? attempt.PayableId : null;
        var applied = await paymentApplier.ApplyPaidAmountAsync(new PaymentApplicationRequest(
            attempt.TenantId,
            attempt.InvoiceId,
            instalmentId,
            attempt.StudentId,
            attempt.Amount,
            attempt.Id,
            AllowOverpaymentCredit: true), ct);

        if (applied.CreditId is Guid creditId && applied.OverpaymentCredited > 0)
        {
            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                TenantId = attempt.TenantId,
                EventName = DomainEvents.OverpaymentCredited,
                OccurredAtUtc = now,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    tenantId = attempt.TenantId,
                    studentId = attempt.StudentId,
                    creditId,
                    amount = PaymentsWire.ToWire(applied.OverpaymentCredited),
                    sourceAttemptId = attempt.Id,
                    occurredAt = now,
                }, JsonOptions),
            });
        }

        var seq = await db.ReceiptSequences.FirstOrDefaultAsync(s => s.TenantId == attempt.TenantId, ct);
        if (seq is null)
        {
            seq = new ReceiptSequence { TenantId = attempt.TenantId, NextNumber = 1 };
            db.ReceiptSequences.Add(seq);
        }

        var receiptNumber = seq.NextNumber;
        seq.NextNumber++;

        var payable = await payableLookup.GetPayableAsync(PayableTypes.Invoice, attempt.InvoiceId, ct);
        var tenant = await db.Tenants.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == attempt.TenantId, ct);
        var institutionName = tenant?.Name ?? PilotSeed.DisplayName;
        var initials = ReceiptPdfGenerator.InitialsFromDisplayName(payable?.StudentDisplayName);
        var lines = (payable?.Lines ?? [])
            .Select(l => new ReceiptLineModel(l.FeeHeadCode, PaymentsWire.ToWire(l.Amount)))
            .ToList();
        if (lines.Count == 0)
            lines.Add(new ReceiptLineModel("Payment", PaymentsWire.ToWire(attempt.Amount)));

        var verifyToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var receipt = new PaymentReceipt
        {
            Id = Guid.NewGuid(),
            TenantId = attempt.TenantId,
            PaymentAttemptId = attempt.Id,
            InvoiceId = attempt.InvoiceId,
            StudentId = attempt.StudentId,
            ReceiptNumber = receiptNumber,
            Amount = attempt.Amount,
            Currency = attempt.Currency,
            Channel = attempt.AdapterCode,
            GatewayTxnId = attempt.GatewayTxnId,
            StudentInitials = initials,
            InstitutionName = institutionName,
            LinesJson = ReceiptPdfGenerator.SerializeLines(lines),
            VerifyToken = verifyToken,
            CreatedAtUtc = now,
        };
        receipt.PdfBytes = BuildPdf(receipt);
        db.PaymentReceipts.Add(receipt);
        attempt.ReceiptId = receipt.Id;

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = attempt.TenantId,
            EventName = DomainEvents.PaymentSucceeded,
            OccurredAtUtc = now,
            PayloadJson = JsonSerializer.Serialize(new
            {
                tenantId = attempt.TenantId,
                attemptId = attempt.Id,
                paymentId = attempt.Id,
                receiptId = receipt.Id,
                receiptNumber = receipt.ReceiptNumber,
                amount = PaymentsWire.ToWire(attempt.Amount),
                channel = attempt.AdapterCode,
                gatewayTxnId = attempt.GatewayTxnId,
                studentId = attempt.StudentId,
                invoiceId = attempt.InvoiceId,
                occurredAt = now,
            }, JsonOptions),
        });

        await db.SaveChangesAsync(ct);
        if (tx is not null)
            await tx.CommitAsync(ct);
        return ToDto(attempt);
    }

    private byte[] BuildPdf(PaymentReceipt receipt)
    {
        var verifyUrl = $"{options.Value.PublicBaseUrl.TrimEnd('/')}/api/v1/payments/receipts/verify/{receipt.VerifyToken}";
        return ReceiptPdfGenerator.Generate(new ReceiptPdfModel
        {
            InstitutionName = receipt.InstitutionName,
            ReceiptNumber = receipt.ReceiptNumber,
            AmountWire = PaymentsWire.ToWire(receipt.Amount),
            Channel = receipt.Channel,
            GatewayTxnId = receipt.GatewayTxnId,
            StudentInitials = receipt.StudentInitials,
            PaidAtUtc = receipt.CreatedAtUtc,
            VerifyUrl = verifyUrl,
            Lines = ReceiptPdfGenerator.DeserializeLines(receipt.LinesJson),
        });
    }

    private string CreateSignedPayLinkToken(Guid tenantId, Guid invoiceId, DateTimeOffset expires)
    {
        var payload = $"{tenantId:D}.{invoiceId:D}.{expires.ToUnixTimeSeconds()}";
        var key = Encoding.UTF8.GetBytes(options.Value.PayLinkSigningKey);
        var sig = Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var raw = $"{payload}.{sig}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private void EnsureAdapterConfigs(Guid tenantId)
    {
        var existing = db.TenantAdapterConfigs.IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId)
            .Select(c => c.AdapterCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var code in AdapterCodes.All)
        {
            if (existing.Contains(code))
                continue;

            var enabled = options.Value.Adapters.TryGetValue(code, out var cfg) ? cfg.Enabled : true;
            db.TenantAdapterConfigs.Add(new TenantAdapterConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AdapterCode = code,
                Enabled = enabled,
                Tier = AdapterCodes.TierFor(code),
                SecretRef = PilotSeed.AdapterSecretRef(code),
            });
        }

        if (db.ChangeTracker.HasChanges())
            db.SaveChanges();
    }

    private Guid RequireTenant(Guid? queryTenantId = null)
    {
        if (queryTenantId is Guid q)
            tenantContext.SetTenant(q);

        if (tenantContext.TenantId is Guid tid)
            return tid;

        throw new PaymentsDomainException(
            "TENANT_REQUIRED",
            "Tenant context is required.",
            "errors.tenant_required");
    }

    private static PaymentAttemptDto ToDto(PaymentAttempt a) => new(
        a.Id,
        a.Status,
        a.PayableType,
        a.PayableId,
        a.InvoiceId,
        a.StudentId,
        PaymentsWire.ToWire(a.Amount),
        a.Currency,
        a.AdapterCode,
        a.IdempotencyKey,
        a.CheckoutUrl,
        a.GatewayRef,
        a.GatewayTxnId,
        a.ReceiptId,
        a.CreatedAtUtc,
        a.UpdatedAtUtc);

    private PaymentReceiptDto ToReceiptDto(PaymentReceipt r) => new(
        r.Id,
        r.ReceiptNumber,
        r.PaymentAttemptId,
        r.InvoiceId,
        PaymentsWire.ToWire(r.Amount),
        r.Currency,
        r.Channel,
        r.GatewayTxnId,
        r.StudentInitials,
        r.InstitutionName,
        $"{options.Value.PublicBaseUrl.TrimEnd('/')}/api/v1/payments/receipts/verify/{r.VerifyToken}",
        r.CreatedAtUtc,
        r.PdfBytes is { Length: > 0 });
}
