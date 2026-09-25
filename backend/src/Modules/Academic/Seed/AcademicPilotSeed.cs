using FeePlatform.Modules.IdentityTenancy.Seed;
using Microsoft.EntityFrameworkCore;

namespace FeePlatform.Modules.Academic.Seed;

/// <summary>
/// Anonymised pilot academic sample aligned with CONTRACT §5.3–§5.6.
/// Agent A applies seeds in migrations/runners; C owns the content shape.
/// </summary>
public static class AcademicPilotSeed
{
    public static readonly Guid ProgramBscsId = Guid.Parse("22222222-2222-2222-2222-222222220001");
    public static readonly Guid ProgramBbaId = Guid.Parse("22222222-2222-2222-2222-222222220002");
    public static readonly Guid ProgramBsseId = Guid.Parse("22222222-2222-2222-2222-222222220003");

    public static readonly Guid IntakeBscs2024Id = Guid.Parse("33333333-3333-3333-3333-333333330001");
    public static readonly Guid TermFall2026Id = Guid.Parse("44444444-4444-4444-4444-444444440001");
    public static readonly Guid TermSpring2027Id = Guid.Parse("44444444-4444-4444-4444-444444440002");

    public static readonly Guid CourseCs101Id = Guid.Parse("55555555-5555-5555-5555-555555550001");
    public static readonly Guid CourseCs201Id = Guid.Parse("55555555-5555-5555-5555-555555550002");
    public static readonly Guid CourseBa101Id = Guid.Parse("55555555-5555-5555-5555-555555550003");

    public static readonly Guid Student1Id = Guid.Parse("66666666-6666-6666-6666-666666660001");
    public static readonly Guid Student2Id = Guid.Parse("66666666-6666-6666-6666-666666660002");
    public static readonly Guid Guardian1Id = Guid.Parse("77777777-7777-7777-7777-777777770001");
    public static readonly Guid Guardian2Id = Guid.Parse("77777777-7777-7777-7777-777777770002");

    public static async Task EnsureAppliedAsync(DbContext db, CancellationToken ct = default)
    {
        var tenantId = PilotSeed.TenantId;
        var now = DateTimeOffset.UtcNow;

        if (!await db.Set<Program>().IgnoreQueryFilters().AnyAsync(p => p.TenantId == tenantId, ct))
        {
            db.Set<Program>().AddRange(
                new Program
                {
                    Id = ProgramBscsId,
                    TenantId = tenantId,
                    Code = "BSCS",
                    Name = "BS Computer Science",
                    ExternalId = "ERP-PROG-BSCS",
                    CreatedAtUtc = now,
                },
                new Program
                {
                    Id = ProgramBbaId,
                    TenantId = tenantId,
                    Code = "BBA",
                    Name = "BBA",
                    ExternalId = "ERP-PROG-BBA",
                    CreatedAtUtc = now,
                },
                new Program
                {
                    Id = ProgramBsseId,
                    TenantId = tenantId,
                    Code = "BSSE",
                    Name = "BS Software Engineering",
                    ExternalId = "ERP-PROG-BSSE",
                    CreatedAtUtc = now,
                });
        }

        if (!await db.Set<Intake>().IgnoreQueryFilters().AnyAsync(i => i.TenantId == tenantId, ct))
        {
            db.Set<Intake>().Add(new Intake
            {
                Id = IntakeBscs2024Id,
                TenantId = tenantId,
                ProgramId = ProgramBscsId,
                Code = "BSCS-2024",
                Name = "BS Computer Science — 2024 intake",
                ExternalId = "ERP-INT-BSCS-2024",
                Year = 2024,
                CreatedAtUtc = now,
            });
        }

        if (!await db.Set<Term>().IgnoreQueryFilters().AnyAsync(t => t.TenantId == tenantId, ct))
        {
            db.Set<Term>().AddRange(
                new Term
                {
                    Id = TermFall2026Id,
                    TenantId = tenantId,
                    Code = "FALL-2026",
                    Name = "Fall 2026",
                    ExternalId = "ERP-TERM-FALL-2026",
                    StartDate = new DateOnly(2026, 9, 1),
                    EndDate = new DateOnly(2027, 1, 15),
                    CreatedAtUtc = now,
                },
                new Term
                {
                    Id = TermSpring2027Id,
                    TenantId = tenantId,
                    Code = "SPRING-2027",
                    Name = "Spring 2027",
                    ExternalId = "ERP-TERM-SPRING-2027",
                    StartDate = new DateOnly(2027, 2, 1),
                    EndDate = new DateOnly(2027, 6, 15),
                    CreatedAtUtc = now,
                });
        }

        if (!await db.Set<Course>().IgnoreQueryFilters().AnyAsync(c => c.TenantId == tenantId, ct))
        {
            db.Set<Course>().AddRange(
                new Course
                {
                    Id = CourseCs101Id,
                    TenantId = tenantId,
                    Code = "CS101",
                    Name = "Intro to Computing",
                    ExternalId = "ERP-CRS-CS101",
                    CreditHours = 3m,
                    Category = "Core",
                    CreatedAtUtc = now,
                },
                new Course
                {
                    Id = CourseCs201Id,
                    TenantId = tenantId,
                    Code = "CS201",
                    Name = "Data Structures",
                    ExternalId = "ERP-CRS-CS201",
                    CreditHours = 4m,
                    Category = "Core",
                    CreatedAtUtc = now,
                },
                new Course
                {
                    Id = CourseBa101Id,
                    TenantId = tenantId,
                    Code = "BA101",
                    Name = "Principles of Management",
                    ExternalId = "ERP-CRS-BA101",
                    CreditHours = 3m,
                    Category = "Core",
                    CreatedAtUtc = now,
                });
        }

        if (!await db.Set<Student>().IgnoreQueryFilters().AnyAsync(s => s.TenantId == tenantId, ct))
        {
            db.Set<Student>().AddRange(
                new Student
                {
                    Id = Student1Id,
                    TenantId = tenantId,
                    ExternalId = "STU-PILOT-0001",
                    DisplayName = "Student One",
                    ProgramId = ProgramBscsId,
                    IntakeId = IntakeBscs2024Id,
                    AttributesJson = """{"year":3}""",
                    CreatedAtUtc = now,
                },
                new Student
                {
                    Id = Student2Id,
                    TenantId = tenantId,
                    ExternalId = "STU-PILOT-0002",
                    DisplayName = "Student Two",
                    ProgramId = ProgramBbaId,
                    AttributesJson = """{"year":2}""",
                    CreatedAtUtc = now,
                });
        }

        if (!await db.Set<Guardian>().IgnoreQueryFilters().AnyAsync(g => g.TenantId == tenantId, ct))
        {
            db.Set<Guardian>().AddRange(
                new Guardian
                {
                    Id = Guardian1Id,
                    TenantId = tenantId,
                    ExternalId = "GRD-PILOT-0001",
                    DisplayName = "Guardian One",
                    MobileE164 = PilotSeed.GuardianMobileFake,
                    CreatedAtUtc = now,
                },
                new Guardian
                {
                    Id = Guardian2Id,
                    TenantId = tenantId,
                    ExternalId = "GRD-PILOT-0002",
                    DisplayName = "Guardian Two",
                    MobileE164 = "+923001111002",
                    CreatedAtUtc = now,
                });

            db.Set<StudentGuardian>().AddRange(
                new StudentGuardian
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888880001"),
                    TenantId = tenantId,
                    StudentId = Student1Id,
                    GuardianId = Guardian1Id,
                    Relation = "Parent",
                    Verified = true,
                },
                new StudentGuardian
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888880002"),
                    TenantId = tenantId,
                    StudentId = Student2Id,
                    GuardianId = Guardian2Id,
                    Relation = "Parent",
                    Verified = true,
                });
        }

        if (!await db.Set<Enrolment>().IgnoreQueryFilters().AnyAsync(e => e.TenantId == tenantId, ct))
        {
            db.Set<Enrolment>().AddRange(
                new Enrolment
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999990001"),
                    TenantId = tenantId,
                    StudentId = Student1Id,
                    CourseId = CourseCs101Id,
                    TermId = TermFall2026Id,
                    IsRepeat = false,
                    Status = EnrolmentStatuses.Enrolled,
                    AddedDate = new DateOnly(2026, 8, 15),
                    ExternalId = "ERP-ENR-0001",
                    CreatedAtUtc = now,
                },
                new Enrolment
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999990002"),
                    TenantId = tenantId,
                    StudentId = Student1Id,
                    CourseId = CourseCs201Id,
                    TermId = TermFall2026Id,
                    IsRepeat = false,
                    Status = EnrolmentStatuses.Enrolled,
                    AddedDate = new DateOnly(2026, 8, 15),
                    ExternalId = "ERP-ENR-0002",
                    CreatedAtUtc = now,
                },
                new Enrolment
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999990003"),
                    TenantId = tenantId,
                    StudentId = Student2Id,
                    CourseId = CourseBa101Id,
                    TermId = TermFall2026Id,
                    IsRepeat = false,
                    Status = EnrolmentStatuses.Enrolled,
                    AddedDate = new DateOnly(2026, 8, 20),
                    ExternalId = "ERP-ENR-0003",
                    CreatedAtUtc = now,
                });
        }

        await db.SaveChangesAsync(ct);
    }
}
