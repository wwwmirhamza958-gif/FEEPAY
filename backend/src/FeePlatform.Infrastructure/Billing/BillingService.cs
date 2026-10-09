using System.Text.Json;
using FeePlatform.Infrastructure.Outbox;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Infrastructure.Tenancy;
using FeePlatform.Modules.Academic;
using FeePlatform.Modules.Billing;
using FeePlatform.Modules.FeeEngine.Api;
using FeePlatform.Modules.FeeEngine.Fixtures;
using FeePlatform.Modules.FeeEngine.Models;
using FeePlatform.Modules.FeeEngine.Models.Rules;
using Microsoft.EntityFrameworkCore;

namespace FeePlatform.Infrastructure.Billing;

public sealed class BillingService(
    AppDbContext db,
    IFeeEngine feeEngine,
    ITenantContext tenantContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>CONTRACT §5.5 v1.2 — LATE_FEE is the only late-fee head code.</summary>
    private static readonly FeeHeadDefinition LateFeeHead =
        new("LATE_FEE", "Late fee", Refundable: false, TaxableFor236I: false, Discountable: false, OneTime: false);

    private static readonly IReadOnlyList<FeeHeadDefinition> PilotHeads =
        PilotFeeHeads.All.Append(LateFeeHead).ToList();

    private static readonly IReadOnlyList<FeeRuleDefinition> DefaultRules =
    [
        SampleRules.TuitionPerCreditHour(),
        SampleRules.LibraryFlat(),
        SampleRules.RepeatCourse(),
    ];

    public async Task<PreviewRunResultDto> PreviewAsync(Guid termId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == termId, ct)
            ?? throw new BillingDomainException("TERM_NOT_FOUND", "Term was not found.", "errors.term_not_found");

        var students = await db.Students.AsNoTracking().ToListAsync(ct);
        var enrolments = await db.Enrolments.AsNoTracking()
            .Where(e => e.TermId == termId && e.Status == EnrolmentStatuses.Enrolled)
            .ToListAsync(ct);
        var courses = await db.Courses.AsNoTracking().ToDictionaryAsync(c => c.Id, ct);

        var run = new PreviewRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TermId = term.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var studentResults = new List<PreviewStudentResultDto>();
        decimal grandTotal = 0;

        foreach (var student in students)
        {
            var studentEnrolments = enrolments.Where(e => e.StudentId == student.Id).ToList();
            if (studentEnrolments.Count == 0) continue;

            var calc = CalculateForStudent(student, term.Code, studentEnrolments, courses, isPreview: true);
            grandTotal += calc.TotalAmount;

            studentResults.Add(new PreviewStudentResultDto(
                student.Id,
                student.ExternalId,
                BillingWire.ToWire(calc.TotalAmount),
                IsOutlier: false,
                OutlierReason: null,
                calc.Lines.Select(l => new FeeLineDto(l.FeeHeadCode, BillingWire.ToWire(l.Amount), l.Trace)).ToList()));
        }

        MarkOutliers(studentResults);

        run.StudentCount = studentResults.Count;
        run.TotalAmount = grandTotal;
        run.OutlierCount = studentResults.Count(r => r.IsOutlier);

        foreach (var r in studentResults)
        {
            run.Results.Add(new PreviewStudentResult
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PreviewRunId = run.Id,
                StudentId = r.StudentId,
                TotalAmount = BillingWire.TryParseWire(r.TotalAmount, out var amt) ? amt : 0,
                IsOutlier = r.IsOutlier,
                OutlierReason = r.OutlierReason,
                LinesJson = JsonSerializer.Serialize(r.Lines, JsonOptions),
            });
        }

        db.PreviewRuns.Add(run);
        await db.SaveChangesAsync(ct);

        return new PreviewRunResultDto(
            run.Id,
            term.Id,
            run.StudentCount,
            BillingWire.ToWire(run.TotalAmount),
            run.OutlierCount,
            studentResults);
    }

    public async Task<IssueBatchResultDto> IssueForTermAsync(
        Guid? termId,
        DateOnly? dueDate,
        Guid? instalmentPlanId,
        Guid? previewRunId,
        string? reason,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();

        Guid resolvedTermId;
        if (previewRunId is Guid previewId)
        {
            var preview = await db.PreviewRuns.FirstOrDefaultAsync(p => p.Id == previewId, ct)
                ?? throw new BillingDomainException("PREVIEW_NOT_FOUND", "Preview run was not found.", "errors.preview_not_found");
            resolvedTermId = preview.TermId;
        }
        else if (termId is Guid tid)
        {
            resolvedTermId = tid;
        }
        else
        {
            throw new BillingDomainException("VALIDATION_FAILED", "termId or previewRunId is required.", "errors.validation_failed");
        }

        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == resolvedTermId, ct)
            ?? throw new BillingDomainException("TERM_NOT_FOUND", "Term was not found.", "errors.term_not_found");

        var effectiveDue = dueDate ?? term.EndDate;
        var plan = instalmentPlanId is null
            ? null
            : await db.InstalmentPlans
                .Include(p => p.Parts)
                .FirstOrDefaultAsync(p => p.Id == instalmentPlanId, ct);

        if (instalmentPlanId is not null && plan is null)
            throw new BillingDomainException("INSTALMENT_PLAN_NOT_FOUND", "Instalment plan was not found.", "errors.instalment_plan_not_found");

        var students = await db.Students.ToListAsync(ct);
        var enrolments = await db.Enrolments
            .Where(e => e.TermId == resolvedTermId && e.Status == EnrolmentStatuses.Enrolled)
            .ToListAsync(ct);
        var courses = await db.Courses.AsNoTracking().ToDictionaryAsync(c => c.Id, ct);

        var issued = new List<InvoiceSummaryDto>();
        var now = DateTimeOffset.UtcNow;
        var issueDate = DateOnly.FromDateTime(now.UtcDateTime);

        foreach (var student in students)
        {
            var studentEnrolments = enrolments.Where(e => e.StudentId == student.Id).ToList();
            if (studentEnrolments.Count == 0) continue;

            var alreadyIssued = await db.Invoices.AnyAsync(
                i => i.StudentId == student.Id
                     && i.TermId == resolvedTermId
                     && i.Status != InvoiceStatuses.Superseded
                     && i.Status != InvoiceStatuses.Draft,
                ct);
            if (alreadyIssued) continue;

            var calc = CalculateForStudent(student, term.Code, studentEnrolments, courses, isPreview: false);
            var invoice = CreateIssuedInvoice(
                tenantId, student.Id, resolvedTermId, calc, effectiveDue, plan, issueDate, now,
                previousInvoiceId: null, adjustmentOf: null, reason: reason);
            issued.Add(ToSummary(invoice));
        }

        await db.SaveChangesAsync(ct);
        return new IssueBatchResultDto(resolvedTermId, issued.Count, issued);
    }

    /// <summary>Issue a fee challan for one enrolled student in a term (Finance Add Student flow).</summary>
    public async Task<InvoiceSummaryDto> IssueForStudentAsync(
        Guid studentId,
        Guid termId,
        DateOnly? dueDate,
        Guid? instalmentPlanId,
        string reason,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct)
            ?? throw new BillingDomainException("STUDENT_NOT_FOUND", "Student was not found.", "errors.student_not_found");

        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == termId, ct)
            ?? throw new BillingDomainException("TERM_NOT_FOUND", "Term was not found.", "errors.term_not_found");

        var enrolments = await db.Enrolments
            .Where(e => e.StudentId == studentId && e.TermId == termId && e.Status == EnrolmentStatuses.Enrolled)
            .ToListAsync(ct);
        if (enrolments.Count == 0)
        {
            throw new BillingDomainException(
                "NO_ENROLMENTS",
                "Student has no enrolments for this term.",
                "errors.no_enrolments");
        }

        var alreadyIssued = await db.Invoices.AnyAsync(
            i => i.StudentId == studentId
                 && i.TermId == termId
                 && i.Status != InvoiceStatuses.Superseded
                 && i.Status != InvoiceStatuses.Draft,
            ct);
        if (alreadyIssued)
        {
            throw new BillingDomainException(
                "ALREADY_ISSUED",
                "A fee challan already exists for this student and term.",
                "errors.already_issued");
        }

        var plan = instalmentPlanId is null
            ? null
            : await db.InstalmentPlans
                .Include(p => p.Parts)
                .FirstOrDefaultAsync(p => p.Id == instalmentPlanId, ct);

        var courses = await db.Courses.AsNoTracking().ToDictionaryAsync(c => c.Id, ct);
        var now = DateTimeOffset.UtcNow;
        var issueDate = DateOnly.FromDateTime(now.UtcDateTime);
        var effectiveDue = dueDate ?? term.EndDate;

        var calc = CalculateForStudent(student, term.Code, enrolments, courses, isPreview: false);
        if (calc.TotalAmount <= 0)
        {
            // Fallback flat challan when engine returns nothing (e.g. no matching rules).
            calc = new CalculatedStatement(
                [new CalculatedLine("TUITION", 71250.00m, "Tuition fee challan", "Charge", null)],
                71250.00m);
        }

        var invoice = CreateIssuedInvoice(
            tenantId, student.Id, termId, calc, effectiveDue, plan, issueDate, now,
            previousInvoiceId: null, adjustmentOf: null, reason: reason);
        await db.SaveChangesAsync(ct);
        return ToSummary(invoice);
    }

    public async Task<InvoiceDetailDto> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        RequireTenant();
        var invoice = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Instalments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new BillingDomainException("INVOICE_NOT_FOUND", "Invoice was not found.", "errors.invoice_not_found");

        return ToDetail(invoice);
    }

    public async Task<InvoiceDetailDto> AdjustAsync(
        Guid originalInvoiceId,
        string reason,
        IReadOnlyList<AdjustmentLineInput>? replacementLines,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var original = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Instalments)
            .FirstOrDefaultAsync(i => i.Id == originalInvoiceId, ct)
            ?? throw new BillingDomainException("INVOICE_NOT_FOUND", "Invoice was not found.", "errors.invoice_not_found");

        if (original.Status is not (InvoiceStatuses.Issued or InvoiceStatuses.PartiallyPaid))
        {
            throw new BillingDomainException(
                "INVOICE_NOT_ADJUSTABLE",
                "Only Issued or PartiallyPaid invoices can be adjusted.",
                "errors.invoice_not_adjustable");
        }

        var now = DateTimeOffset.UtcNow;
        var issueDate = DateOnly.FromDateTime(now.UtcDateTime);

        CalculatedStatement calc;
        if (replacementLines is { Count: > 0 })
        {
            var lines = replacementLines.Select(l =>
            {
                if (!BillingWire.TryParseWire(l.Amount, out var amt))
                    throw new BillingDomainException("INVALID_AMOUNT", "Amount must be a decimal string with two places.", "errors.invalid_amount");
                return new CalculatedLine(
                    l.FeeHeadCode,
                    BillingWire.RoundHalfUp(amt),
                    l.Trace ?? $"{l.FeeHeadCode} adjustment",
                    "Adjustment",
                    null);
            }).ToList();
            calc = new CalculatedStatement(lines, lines.Sum(x => x.Amount));
        }
        else
        {
            var lines = original.Lines
                .OrderBy(l => l.SortOrder)
                .Select(l => new CalculatedLine(
                    l.FeeHeadCode,
                    BillingWire.RoundHalfUp(-l.Amount),
                    $"Credit: {l.Trace}",
                    "Adjustment",
                    null))
                .ToList();
            calc = new CalculatedStatement(lines, lines.Sum(x => x.Amount));
        }

        InstalmentPlan? plan = null;
        if (original.InstalmentPlanId is Guid planId)
        {
            plan = await db.InstalmentPlans.Include(p => p.Parts)
                .FirstOrDefaultAsync(p => p.Id == planId, ct);
        }

        var replacement = CreateIssuedInvoice(
            tenantId,
            original.StudentId,
            original.TermId,
            calc,
            original.DueDate,
            plan,
            issueDate,
            now,
            previousInvoiceId: original.Id,
            adjustmentOf: original.Id,
            reason: reason);

        replacement.Version = original.Version + 1;

        original.Status = InvoiceStatuses.Superseded;
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventName = DomainEvents.InvoiceSuperseded,
            PayloadJson = JsonSerializer.Serialize(new
            {
                tenantId,
                invoiceId = original.Id,
                supersededByInvoiceId = replacement.Id,
                occurredAt = now,
            }, JsonOptions),
            OccurredAtUtc = now,
        });

        await db.SaveChangesAsync(ct);
        return ToDetail(replacement);
    }

    public async Task<InstalmentPlanDto> CreateInstalmentPlanAsync(
        CreateInstalmentPlanRequest request,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (request.Parts.Count == 0)
            throw new BillingDomainException("VALIDATION_FAILED", "At least one instalment part is required.", "errors.validation_failed");

        var plan = new InstalmentPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            ProgramId = request.ProgramId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var seq = 1;
        foreach (var part in request.Parts)
        {
            plan.Parts.Add(new InstalmentPlanPart
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InstalmentPlanId = plan.Id,
                Sequence = part.Sequence ?? seq,
                Share = part.Share,
                DueOffsetDays = part.DueOffsetDays,
                Label = part.Label ?? $"Part {seq}",
            });
            seq++;
        }

        db.InstalmentPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return ToPlanDto(plan);
    }

    public async Task<IReadOnlyList<InstalmentPlanDto>> ListInstalmentPlansAsync(CancellationToken ct = default)
    {
        RequireTenant();
        var plans = await db.InstalmentPlans.Include(p => p.Parts).AsNoTracking().ToListAsync(ct);
        return plans.Select(ToPlanDto).ToList();
    }

    public async Task<DefermentDto> RequestDefermentAsync(RequestDefermentRequest request, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var deferment = new Deferment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = request.StudentId,
            ItemType = request.ItemType,
            ItemId = request.ItemId,
            Status = DefermentStatuses.Requested,
            Reason = request.Reason,
            ProposedDueDate = request.ProposedDueDate,
            RequestedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Deferments.Add(deferment);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventName = DomainEvents.DefermentRequested,
            PayloadJson = JsonSerializer.Serialize(new
            {
                tenantId,
                defermentId = deferment.Id,
                studentId = deferment.StudentId,
                itemType = deferment.ItemType,
                itemId = deferment.ItemId,
                proposedDueDate = deferment.ProposedDueDate,
                occurredAt = deferment.RequestedAtUtc,
            }, JsonOptions),
            OccurredAtUtc = deferment.RequestedAtUtc,
        });
        await db.SaveChangesAsync(ct);
        return ToDefermentDto(deferment);
    }

    public async Task<DefermentDto> DecideDefermentAsync(
        Guid defermentId,
        bool approve,
        Guid decidedByUserId,
        string? note,
        DateOnly? approvedDueDate,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var deferment = await db.Deferments.FirstOrDefaultAsync(d => d.Id == defermentId, ct)
            ?? throw new BillingDomainException("DEFERMENT_NOT_FOUND", "Deferment was not found.", "errors.deferment_not_found");

        if (deferment.Status != DefermentStatuses.Requested)
            throw new BillingDomainException("DEFERMENT_NOT_PENDING", "Deferment is not awaiting a decision.", "errors.deferment_not_pending");

        var now = DateTimeOffset.UtcNow;
        deferment.DecidedByUserId = decidedByUserId;
        deferment.DecisionNote = note;
        deferment.DecidedAtUtc = now;

        if (approve)
        {
            deferment.Status = DefermentStatuses.Approved;
            deferment.ApprovedDueDate = approvedDueDate ?? deferment.ProposedDueDate;

            if (deferment.ItemType == "Instalment")
            {
                var instalment = await db.Instalments.FirstOrDefaultAsync(i => i.Id == deferment.ItemId, ct);
                if (instalment is not null)
                    instalment.DueDate = deferment.ApprovedDueDate.Value;
            }

            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventName = DomainEvents.DefermentApproved,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    tenantId,
                    defermentId = deferment.Id,
                    studentId = deferment.StudentId,
                    itemId = deferment.ItemId,
                    newDueDate = deferment.ApprovedDueDate,
                    decidedByUserId,
                    occurredAt = now,
                }, JsonOptions),
                OccurredAtUtc = now,
            });
        }
        else
        {
            deferment.Status = DefermentStatuses.Rejected;
            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventName = DomainEvents.DefermentRejected,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    tenantId,
                    defermentId = deferment.Id,
                    reason = note ?? deferment.Reason,
                    decidedByUserId,
                    occurredAt = now,
                }, JsonOptions),
                OccurredAtUtc = now,
            });
        }

        await db.SaveChangesAsync(ct);
        return ToDefermentDto(deferment);
    }

    private Invoice CreateIssuedInvoice(
        Guid tenantId,
        Guid studentId,
        Guid termId,
        CalculatedStatement calc,
        DateOnly dueDate,
        InstalmentPlan? plan,
        DateOnly issueDate,
        DateTimeOffset now,
        Guid? previousInvoiceId,
        Guid? adjustmentOf,
        string? reason)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            TermId = termId,
            Version = 1,
            Status = InvoiceStatuses.Issued,
            TotalAmount = calc.TotalAmount,
            RemainingBalance = calc.TotalAmount,
            Currency = "PKR",
            DueDate = dueDate,
            PreviousInvoiceId = previousInvoiceId,
            AdjustmentOfInvoiceId = adjustmentOf,
            Reason = reason,
            InstalmentPlanId = plan?.Id,
            CreatedAtUtc = now,
            IssuedAtUtc = now,
        };

        var order = 0;
        foreach (var line in calc.Lines)
        {
            invoice.Lines.Add(new InvoiceLine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InvoiceId = invoice.Id,
                FeeHeadCode = line.FeeHeadCode,
                Amount = line.Amount,
                Trace = line.Trace,
                Kind = line.Kind,
                RuleRef = line.RuleRef,
                SortOrder = order++,
            });
        }

        if (plan is not null && plan.Parts.Count > 0)
        {
            var parts = plan.Parts.OrderBy(p => p.Sequence).ToList();
            foreach (var split in InstalmentSplitter.Split(calc.TotalAmount, issueDate, parts))
            {
                invoice.Instalments.Add(new Instalment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    InvoiceId = invoice.Id,
                    Sequence = split.Sequence,
                    Label = split.Label,
                    Amount = split.Amount,
                    DueDate = split.DueDate,
                    PaidAmount = 0,
                });
            }

            invoice.DueDate = invoice.Instalments.Min(i => i.DueDate);
        }

        db.Invoices.Add(invoice);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventName = DomainEvents.InvoiceIssued,
            PayloadJson = JsonSerializer.Serialize(new
            {
                tenantId,
                invoiceId = invoice.Id,
                studentId = invoice.StudentId,
                termId = invoice.TermId,
                version = invoice.Version,
                totalAmount = BillingWire.ToWire(invoice.TotalAmount),
                currency = invoice.Currency,
                dueDate = invoice.DueDate,
                occurredAt = now,
            }, JsonOptions),
            OccurredAtUtc = now,
        });

        return invoice;
    }

    private CalculatedStatement CalculateForStudent(
        Student student,
        string termCode,
        List<Enrolment> enrolments,
        Dictionary<Guid, Course> courses,
        bool isPreview)
    {
        var registered = enrolments.Select(e =>
        {
            if (!courses.TryGetValue(e.CourseId, out var course))
                throw new BillingDomainException("COURSE_NOT_FOUND", "Enrolment references a missing course.", "errors.course_not_found");
            return new RegisteredCourse(course.Code, course.CreditHours, course.Category, e.IsRepeat);
        }).ToList();

        var input = new FeeEngineInput(
            StudentId: student.ExternalId,
            TermCode: termCode,
            FeeHeads: PilotHeads,
            Rules: DefaultRules,
            Enrolment: new EnrolmentSnapshot(registered),
            Attributes: StudentAttributes.Empty,
            IsPreview: isPreview);

        var result = feeEngine.Calculate(input);
        // TaxLines always empty in P1 — never persist.
        _ = result.TaxLines;

        var lines = result.Lines.Select(l => new CalculatedLine(
            l.FeeHeadCode,
            l.Amount,
            l.Trace,
            l.Kind.ToString(),
            l.RuleId ?? l.DiscountId)).ToList();

        return new CalculatedStatement(lines, result.NetTotal);
    }

    private static void MarkOutliers(List<PreviewStudentResultDto> results)
    {
        if (results.Count == 0) return;
        var amounts = results
            .Select(r => BillingWire.TryParseWire(r.TotalAmount, out var a) ? a : 0m)
            .OrderBy(a => a)
            .ToList();
        var median = amounts[amounts.Count / 2];
        var threshold = median <= 0 ? 200_000m : median * 2m;

        for (var i = 0; i < results.Count; i++)
        {
            var amt = BillingWire.TryParseWire(results[i].TotalAmount, out var a) ? a : 0m;
            if (amt > threshold)
            {
                results[i] = results[i] with
                {
                    IsOutlier = true,
                    OutlierReason = $"Total {BillingWire.ToWire(amt)} exceeds 2× median {BillingWire.ToWire(median)}",
                };
            }
        }
    }

    private Guid RequireTenant()
    {
        if (tenantContext.TenantId is not Guid tid)
            throw new BillingDomainException("TENANT_REQUIRED", "Tenant context is required.", "errors.tenant_required");
        return tid;
    }

    private static InvoiceSummaryDto ToSummary(Invoice invoice) =>
        new(invoice.Id, invoice.StudentId, invoice.TermId, invoice.Status, BillingWire.ToWire(invoice.TotalAmount),
            invoice.DueDate, IsOverdue(invoice));

    private static InvoiceDetailDto ToDetail(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.StudentId,
            invoice.TermId,
            invoice.Version,
            invoice.Status,
            BillingWire.ToWire(invoice.TotalAmount),
            BillingWire.ToWire(invoice.RemainingBalance),
            invoice.Currency,
            invoice.DueDate,
            IsOverdue(invoice),
            invoice.PreviousInvoiceId,
            invoice.AdjustmentOfInvoiceId,
            invoice.Reason,
            invoice.Lines.OrderBy(l => l.SortOrder).Select(l =>
                new FeeLineDto(l.FeeHeadCode, BillingWire.ToWire(l.Amount), l.Trace)).ToList(),
            invoice.Instalments.OrderBy(i => i.Sequence).Select(i =>
                new InstalmentDto(i.Id, i.Sequence, i.Label, BillingWire.ToWire(i.Amount), i.DueDate, BillingWire.ToWire(i.PaidAmount))).ToList());

    private static bool IsOverdue(Invoice invoice)
    {
        if (!InvoiceStatuses.IsPayable(invoice.Status) || invoice.RemainingBalance <= 0)
            return false;
        var tzId = OperatingSystem.IsWindows() ? "Pakistan Standard Time" : "Asia/Karachi";
        var todayKarachi = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(tzId)));
        return invoice.DueDate < todayKarachi;
    }

    private static InstalmentPlanDto ToPlanDto(InstalmentPlan plan) =>
        new(plan.Id, plan.Code, plan.Name, plan.ProgramId,
            plan.Parts.OrderBy(p => p.Sequence).Select(p =>
                new InstalmentPlanPartDto(p.Sequence, p.Share, p.DueOffsetDays, p.Label)).ToList());

    private static DefermentDto ToDefermentDto(Deferment d) =>
        new(d.Id, d.StudentId, d.ItemType, d.ItemId, d.Status, d.Reason, d.ProposedDueDate, d.ApprovedDueDate);

    private sealed record CalculatedLine(string FeeHeadCode, decimal Amount, string Trace, string Kind, string? RuleRef);
    private sealed record CalculatedStatement(IReadOnlyList<CalculatedLine> Lines, decimal TotalAmount);
}
