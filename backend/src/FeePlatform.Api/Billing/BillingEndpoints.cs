using FeePlatform.Api.Auth;
using FeePlatform.Infrastructure.Billing;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Modules.Billing;
using FeePlatform.Modules.IdentityTenancy;
using Microsoft.EntityFrameworkCore;

namespace FeePlatform.Api.Billing;

/// <summary>
/// Academic + Billing routes aligned with docs/supervisor/REQUESTS_TO_C.md (R-C-E-001…014).
/// </summary>
public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // R-C-E-001 — thin read/update over A's TenantBranding (C does not own Identity tables)
        var tenants = endpoints.MapGroup("/api/v1/tenants").WithTags("Tenants");
        tenants.MapGet("/{tenantId:guid}/branding", GetBranding).WithName("GetTenantBranding");
        tenants.MapMethods("/{tenantId:guid}/branding", ["PATCH"], PatchBranding).WithName("PatchTenantBranding");

        var academic = endpoints.MapGroup("/api/v1/academic").WithTags("Academic");
        // R-C-E-002
        academic.MapGet("/programs", ListPrograms).WithName("ListPrograms");
        academic.MapPost("/programs", CreateProgram).WithName("CreateProgram");
        academic.MapGet("/terms", ListTerms).WithName("ListTerms");
        // R-C-E-011
        academic.MapGet("/students", ListStudents).WithName("ListStudents");
        academic.MapGet("/students/{studentId:guid}", GetStudent).WithName("GetStudent");

        var billing = endpoints.MapGroup("/api/v1/billing").WithTags("Billing");

        // R-C-E-003 — catalog from CONTRACT §5.5 (engine config owned by B; catalog readable here)
        billing.MapGet("/fee-heads", ListFeeHeads).WithName("ListFeeHeads");
        billing.MapPost("/fee-heads", () =>
            ApiErrors.Result(
                StatusCodes.Status501NotImplemented,
                "FEE_HEADS_OWNED_BY_B",
                "Creating fee heads is owned by FeeEngine (Agent B). Catalog is read-only here.",
                "errors.fee_heads_owned_by_b"));

        // R-C-E-004 / R-C-E-005 — stubs until B persists schedules/discounts
        billing.MapGet("/fee-schedules", () => Results.Ok(Array.Empty<object>())).WithName("ListFeeSchedules");
        billing.MapGet("/discount-categories", () => Results.Ok(Array.Empty<object>())).WithName("ListDiscountCategories");
        billing.MapPost("/discount-categories", () =>
            ApiErrors.Result(
                StatusCodes.Status501NotImplemented,
                "DISCOUNTS_OWNED_BY_B",
                "Discount categories are owned by FeeEngine (Agent B).",
                "errors.discounts_owned_by_b"));

        // R-C-E-006
        billing.MapGet("/instalment-plans", ListPlans).WithName("ListInstalmentPlans");
        billing.MapPost("/instalment-plans", CreatePlan).WithName("CreateInstalmentPlan");

        // R-C-E-012
        billing.MapPost("/preview-runs", Preview)
            .WithName("BillingPreviewRuns")
            .Produces<PreviewRunResultDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        // R-C-E-013
        billing.MapPost("/statements/issue", Issue)
            .WithName("BillingStatementsIssue")
            .Produces<IssueBatchResultDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        billing.MapGet("/invoices/{invoiceId:guid}", GetInvoice)
            .WithName("GetInvoice")
            .Produces<InvoiceDetailDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status404NotFound);

        // R-C-E-014
        billing.MapPost("/adjustments", Adjust)
            .WithName("CreateAdjustment")
            .Produces<InvoiceDetailDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status409Conflict);

        billing.MapPost("/deferments", RequestDeferment)
            .WithName("RequestDeferment")
            .Produces<DefermentDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        billing.MapPost("/deferments/{defermentId:guid}/decide", DecideDeferment)
            .WithName("DecideDeferment")
            .Produces<DefermentDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GetBranding(Guid tenantId, AppDbContext db, CancellationToken ct)
    {
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
        {
            return ApiErrors.Result(
                StatusCodes.Status404NotFound,
                "TENANT_NOT_FOUND",
                "Tenant was not found.",
                "errors.tenant_not_found");
        }

        var branding = await db.TenantBrandings.AsNoTracking().FirstOrDefaultAsync(b => b.TenantId == tenantId, ct);
        return Results.Ok(new
        {
            tenantId,
            displayName = tenant.Name,
            brandColourHex = branding?.BrandColourHex ?? "#0B3D91",
            logoObjectKey = branding?.LogoObjectKey,
        });
    }

    private static async Task<IResult> PatchBranding(
        Guid tenantId,
        BrandingPatchRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
        {
            return ApiErrors.Result(
                StatusCodes.Status404NotFound,
                "TENANT_NOT_FOUND",
                "Tenant was not found.",
                "errors.tenant_not_found");
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            tenant.Name = request.DisplayName.Trim();

        var branding = await db.TenantBrandings.FirstOrDefaultAsync(b => b.TenantId == tenantId, ct);
        if (branding is null)
        {
            branding = new TenantBranding { TenantId = tenantId, BrandColourHex = "#0B3D91" };
            db.TenantBrandings.Add(branding);
        }

        if (!string.IsNullOrWhiteSpace(request.BrandColourHex))
            branding.BrandColourHex = request.BrandColourHex.Trim();
        if (request.LogoObjectKey is not null)
            branding.LogoObjectKey = request.LogoObjectKey;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            tenantId,
            displayName = tenant.Name,
            brandColourHex = branding.BrandColourHex,
            logoObjectKey = branding.LogoObjectKey,
        });
    }

    private static async Task<IResult> ListPrograms(AppDbContext db, CancellationToken ct)
    {
        var items = await db.Programs.AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new { p.Id, p.Code, p.Name, p.ExternalId })
            .ToListAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateProgram(
        CreateProgramRequest request,
        AppDbContext db,
        FeePlatform.Infrastructure.Tenancy.ITenantContext tenantContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiErrors.Result(
                StatusCodes.Status400BadRequest,
                "VALIDATION_FAILED",
                "One or more fields are invalid.",
                "errors.validation_failed",
                [new ApiErrorDetail("code", "REQUIRED", "code and name are required.", "errors.field.required")]);
        }

        if (tenantContext.TenantId is not Guid tenantId)
        {
            return ApiErrors.Result(
                StatusCodes.Status400BadRequest,
                "TENANT_REQUIRED",
                "Tenant context is required.",
                "errors.tenant_required");
        }

        var program = new Modules.Academic.Program
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            ExternalId = request.ExternalId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Programs.Add(program);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { program.Id, program.Code, program.Name, program.ExternalId });
    }

    private static async Task<IResult> ListTerms(AppDbContext db, CancellationToken ct)
    {
        var items = await db.Terms.AsNoTracking()
            .OrderBy(t => t.Code)
            .Select(t => new { t.Id, t.Code, t.Name, t.StartDate, t.EndDate, t.ExternalId })
            .ToListAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> ListStudents(string? q, AppDbContext db, CancellationToken ct)
    {
        var query = db.Students.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s =>
                s.ExternalId.Contains(term) || s.DisplayName.Contains(term));
        }

        var items = await query
            .OrderBy(s => s.ExternalId)
            .Select(s => new { s.Id, s.ExternalId, s.DisplayName, s.ProgramId, s.IntakeId })
            .Take(100)
            .ToListAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetStudent(Guid studentId, AppDbContext db, CancellationToken ct)
    {
        var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null)
        {
            return ApiErrors.Result(
                StatusCodes.Status404NotFound,
                "STUDENT_NOT_FOUND",
                "Student was not found.",
                "errors.student_not_found");
        }

        var enrolments = await db.Enrolments.AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => new { e.Id, e.CourseId, e.TermId, e.Status, e.IsRepeat })
            .ToListAsync(ct);

        return Results.Ok(new
        {
            student.Id,
            student.ExternalId,
            student.DisplayName,
            student.ProgramId,
            student.IntakeId,
            enrolments,
        });
    }

    private static IResult ListFeeHeads() =>
        Results.Ok(FeeHeadCatalog.All);

    private static async Task<IResult> Preview(PreviewRequest request, BillingService billing, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await billing.PreviewAsync(request.TermId, ct));
        }
        catch (BillingDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> Issue(IssueStatementsRequest request, BillingService billing, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return ApiErrors.Result(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_FAILED",
                    "One or more fields are invalid.",
                    "errors.validation_failed",
                    [new ApiErrorDetail("reason", "REQUIRED", "reason is required to issue statements.", "errors.field.required")]);
            }

            return Results.Ok(await billing.IssueForTermAsync(
                request.TermId,
                request.DueDate,
                request.InstalmentPlanId,
                request.PreviewRunId,
                request.Reason,
                ct));
        }
        catch (BillingDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> GetInvoice(Guid invoiceId, BillingService billing, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await billing.GetInvoiceAsync(invoiceId, ct));
        }
        catch (BillingDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> Adjust(CreateAdjustmentRequest request, BillingService billing, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return ApiErrors.Result(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_FAILED",
                    "One or more fields are invalid.",
                    "errors.validation_failed",
                    [new ApiErrorDetail("reason", "REQUIRED", "Reason is required.", "errors.field.required")]);
            }

            return Results.Ok(await billing.AdjustAsync(request.InvoiceId, request.Reason, request.Lines, ct));
        }
        catch (BillingDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> ListPlans(BillingService billing, CancellationToken ct) =>
        Results.Ok(await billing.ListInstalmentPlansAsync(ct));

    private static async Task<IResult> CreatePlan(CreateInstalmentPlanRequest request, BillingService billing, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await billing.CreateInstalmentPlanAsync(request, ct));
        }
        catch (BillingDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> RequestDeferment(RequestDefermentRequest request, BillingService billing, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await billing.RequestDefermentAsync(request, ct));
        }
        catch (BillingDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> DecideDeferment(
        Guid defermentId,
        DecideDefermentRequest request,
        BillingService billing,
        CancellationToken ct)
    {
        try
        {
            return Results.Ok(await billing.DecideDefermentAsync(
                defermentId, request.Approve, request.DecidedByUserId, request.Note, request.ApprovedDueDate, ct));
        }
        catch (BillingDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static IResult DomainError(BillingDomainException ex)
    {
        var status = ex.Code switch
        {
            "INVOICE_NOT_FOUND" or "TERM_NOT_FOUND" or "DEFERMENT_NOT_FOUND" or "INSTALMENT_PLAN_NOT_FOUND"
                or "STUDENT_NOT_FOUND" or "COURSE_NOT_FOUND" or "PREVIEW_NOT_FOUND" => StatusCodes.Status404NotFound,
            "INVOICE_NOT_ADJUSTABLE" or "DEFERMENT_NOT_PENDING" => StatusCodes.Status409Conflict,
            "TENANT_REQUIRED" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };
        return ApiErrors.Result(status, ex.Code, ex.Message, ex.MessageKey);
    }
}

public sealed record BrandingPatchRequest(string? DisplayName, string? BrandColourHex, string? LogoObjectKey);
public sealed record CreateProgramRequest(string Code, string Name, string? ExternalId);
public sealed record CreateAdjustmentRequest(
    Guid InvoiceId,
    string Reason,
    IReadOnlyList<AdjustmentLineInput>? Lines);

/// <summary>CONTRACT §5.5 fee-head catalog (read-only until B owns mutable config).</summary>
public static class FeeHeadCatalog
{
    public static readonly object[] All =
    [
        new { code = "TUITION", name = "Tuition", refundable = true, taxableFor236I = true, discountable = true, oneTime = false },
        new { code = "ADMISSION", name = "Admission", refundable = false, taxableFor236I = false, discountable = false, oneTime = true },
        new { code = "REGISTRATION", name = "Registration", refundable = false, taxableFor236I = false, discountable = false, oneTime = false },
        new { code = "EXAM", name = "Examination", refundable = false, taxableFor236I = false, discountable = false, oneTime = false },
        new { code = "LAB", name = "Laboratory", refundable = false, taxableFor236I = false, discountable = true, oneTime = false },
        new { code = "LIBRARY", name = "Library", refundable = false, taxableFor236I = false, discountable = false, oneTime = false },
        new { code = "HOSTEL", name = "Hostel", refundable = true, taxableFor236I = false, discountable = false, oneTime = false },
        new { code = "TRANSPORT", name = "Transport", refundable = false, taxableFor236I = false, discountable = false, oneTime = false },
        new { code = "SECURITY", name = "Security Deposit", refundable = true, taxableFor236I = false, discountable = false, oneTime = true },
        new { code = "REPEAT", name = "Repeat Course", refundable = false, taxableFor236I = true, discountable = false, oneTime = false },
        new { code = "LATE_FEE", name = "Late fee", refundable = false, taxableFor236I = false, discountable = false, oneTime = false },
    ];
}
