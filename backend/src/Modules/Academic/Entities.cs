using FeePlatform.Modules.IdentityTenancy;

namespace FeePlatform.Modules.Academic;

public sealed class Program : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class Intake : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProgramId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public int Year { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class Term : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class Course : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public decimal CreditHours { get; set; }
    public string Category { get; set; } = "Core";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class Student : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid ProgramId { get; set; }
    public Guid? IntakeId { get; set; }
    /// <summary>JSON bag for non-canonical attributes (never real CNIC/phone in seed).</summary>
    public string AttributesJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class Guardian : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? ExternalId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Fake/anonymised E.164 only in non-prod seeds (CONTRACT §5.6).</summary>
    public string MobileE164 { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class StudentGuardian : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid GuardianId { get; set; }
    public string Relation { get; set; } = "Parent";
    public bool Verified { get; set; }
}

public sealed class Enrolment : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid TermId { get; set; }
    public bool IsRepeat { get; set; }
    /// <summary>Enrolment lifecycle: Enrolled | Dropped.</summary>
    public string Status { get; set; } = EnrolmentStatuses.Enrolled;
    public DateOnly? AddedDate { get; set; }
    public DateOnly? DroppedDate { get; set; }
    public string? ExternalId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public static class EnrolmentStatuses
{
    public const string Enrolled = "Enrolled";
    public const string Dropped = "Dropped";
}
