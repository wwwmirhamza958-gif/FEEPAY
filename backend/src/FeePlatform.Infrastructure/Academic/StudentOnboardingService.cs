using FeePlatform.Infrastructure.Billing;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Infrastructure.Tenancy;
using FeePlatform.Modules.Academic;
using FeePlatform.Modules.Billing;
using Microsoft.EntityFrameworkCore;

namespace FeePlatform.Infrastructure.Academic;

/// <summary>
/// Finance Add Student: create student, link father/parent by phone (verified), enrol, issue challan.
/// </summary>
public sealed class StudentOnboardingService(
    AppDbContext db,
    BillingService billing,
    ITenantContext tenantContext)
{
    public async Task<AddStudentResult> AddStudentWithParentAsync(
        AddStudentRequest request,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (string.IsNullOrWhiteSpace(request.ExternalId) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new BillingDomainException(
                "VALIDATION_FAILED",
                "Student number and name are required.",
                "errors.validation_failed");
        }

        var mobile = NormalizeMobile(request.FatherMobile);
        if (mobile.Length < 12)
        {
            throw new BillingDomainException(
                "VALIDATION_FAILED",
                "Enter a Pakistani father/parent mobile number starting with 3.",
                "errors.validation_failed");
        }

        var program = await db.Programs.FirstOrDefaultAsync(p => p.Id == request.ProgramId, ct)
            ?? throw new BillingDomainException("PROGRAM_NOT_FOUND", "Program was not found.", "errors.program_not_found");

        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == request.TermId, ct)
            ?? throw new BillingDomainException("TERM_NOT_FOUND", "Term was not found.", "errors.term_not_found");

        var externalId = request.ExternalId.Trim();
        if (await db.Students.AnyAsync(s => s.ExternalId == externalId, ct))
        {
            throw new BillingDomainException(
                "STUDENT_EXISTS",
                "A student with this student number already exists.",
                "errors.student_exists");
        }

        var course = await db.Courses.AsNoTracking()
            .OrderBy(c => c.Code)
            .FirstOrDefaultAsync(ct)
            ?? throw new BillingDomainException(
                "COURSE_NOT_FOUND",
                "No courses exist for this institute. Add a course before creating students.",
                "errors.course_not_found");

        var now = DateTimeOffset.UtcNow;
        var student = new Student
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExternalId = externalId,
            DisplayName = request.DisplayName.Trim(),
            ProgramId = program.Id,
            IntakeId = request.IntakeId,
            AttributesJson = "{}",
            CreatedAtUtc = now,
        };
        db.Students.Add(student);

        var guardian = await db.Guardians
            .FirstOrDefaultAsync(g => g.MobileE164 == mobile, ct);
        if (guardian is null)
        {
            guardian = new Guardian
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ExternalId = $"GRD-{externalId}",
                DisplayName = string.IsNullOrWhiteSpace(request.FatherDisplayName)
                    ? "Parent"
                    : request.FatherDisplayName.Trim(),
                MobileE164 = mobile,
                CreatedAtUtc = now,
            };
            db.Guardians.Add(guardian);
        }

        var alreadyLinked = await db.StudentGuardians.AnyAsync(
            sg => sg.StudentId == student.Id && sg.GuardianId == guardian.Id,
            ct);
        if (!alreadyLinked)
        {
            db.StudentGuardians.Add(new StudentGuardian
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StudentId = student.Id,
                GuardianId = guardian.Id,
                Relation = string.IsNullOrWhiteSpace(request.Relation) ? "Father" : request.Relation.Trim(),
                Verified = true, // required for parent OTP login
            });
        }

        db.Enrolments.Add(new Enrolment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = student.Id,
            CourseId = course.Id,
            TermId = term.Id,
            IsRepeat = false,
            Status = EnrolmentStatuses.Enrolled,
            AddedDate = DateOnly.FromDateTime(now.UtcDateTime),
            ExternalId = $"ENR-{externalId}",
            CreatedAtUtc = now,
        });

        await db.SaveChangesAsync(ct);

        var due = request.DueDate ?? DateOnly.FromDateTime(now.UtcDateTime).AddDays(30);
        var invoice = await billing.IssueForStudentAsync(
            student.Id,
            term.Id,
            due,
            instalmentPlanId: null,
            reason: request.IssueReason ?? "Challan on student registration",
            ct);

        return new AddStudentResult(
            student.Id,
            student.ExternalId,
            student.DisplayName,
            guardian.Id,
            guardian.MobileE164,
            invoice.InvoiceId,
            invoice.TotalAmount,
            invoice.DueDate,
            invoice.Status);
    }

    private Guid RequireTenant()
    {
        if (tenantContext.TenantId is not Guid tid)
            throw new BillingDomainException("TENANT_REQUIRED", "Tenant context is required.", "errors.tenant_required");
        return tid;
    }

    private static string NormalizeMobile(string mobile)
    {
        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("92", StringComparison.Ordinal) && digits.Length == 12) return "+" + digits;
        if (digits.StartsWith('0') && digits.Length == 11) return "+92" + digits[1..];
        if (digits.Length == 10 && digits.StartsWith('3')) return "+92" + digits;
        return mobile.Trim();
    }
}

public sealed record AddStudentRequest(
    string ExternalId,
    string DisplayName,
    Guid ProgramId,
    Guid TermId,
    string FatherMobile,
    string? FatherDisplayName = null,
    string? Relation = "Father",
    Guid? IntakeId = null,
    DateOnly? DueDate = null,
    string? IssueReason = null);

public sealed record AddStudentResult(
    Guid StudentId,
    string ExternalId,
    string DisplayName,
    Guid GuardianId,
    string FatherMobileE164,
    Guid InvoiceId,
    string FeeAmount,
    DateOnly DueDate,
    string PaymentStatus);
