namespace FeePlatform.Modules.FeeEngine.Models;

public sealed record RegisteredCourse(
    string CourseCode,
    decimal CreditHours,
    string? Category = null,
    bool IsRepeat = false);

/// <summary>Enrolment facts the engine needs — no DB access.</summary>
public sealed record EnrolmentSnapshot(
    IReadOnlyList<RegisteredCourse> Courses,
    bool IsFirstTerm = false,
    int MonthCount = 1,
    IReadOnlySet<string>? PreviouslyChargedOneTimeHeads = null)
{
    /// <summary>Non-repeat credit hours (typical tuition base).</summary>
    public decimal CreditHours => Courses.Where(c => !c.IsRepeat).Sum(c => c.CreditHours);

    public decimal RepeatCreditHours => Courses.Where(c => c.IsRepeat).Sum(c => c.CreditHours);

    public int NonRepeatCourseCount => Courses.Count(c => !c.IsRepeat);

    public int RepeatCourseCount => Courses.Count(c => c.IsRepeat);

    public bool WasChargedOneTime(string feeHeadCode) =>
        PreviouslyChargedOneTimeHeads is not null
        && PreviouslyChargedOneTimeHeads.Contains(feeHeadCode);
}
