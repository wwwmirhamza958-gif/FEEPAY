using FeePlatform.Modules.FeeEngine.Models;
using FeePlatform.Modules.FeeEngine.Models.Discounts;
using FeePlatform.Modules.FeeEngine.Models.Rules;

namespace FeePlatform.Modules.FeeEngine.Fixtures;

/// <summary>Canonical pilot fee heads (CONTRACT §5.5) for fixtures and samples.</summary>
public static class PilotFeeHeads
{
    public static FeeHeadDefinition Tuition { get; } = new("TUITION", "Tuition", true, true, true, false);
    public static FeeHeadDefinition Admission { get; } = new("ADMISSION", "Admission", false, false, false, true);
    public static FeeHeadDefinition Registration { get; } = new("REGISTRATION", "Registration", false, false, false, false);
    public static FeeHeadDefinition Exam { get; } = new("EXAM", "Examination", false, false, false, false);
    public static FeeHeadDefinition Lab { get; } = new("LAB", "Laboratory", false, false, true, false);
    public static FeeHeadDefinition Library { get; } = new("LIBRARY", "Library", false, false, false, false);
    public static FeeHeadDefinition Hostel { get; } = new("HOSTEL", "Hostel", true, false, false, false);
    public static FeeHeadDefinition Transport { get; } = new("TRANSPORT", "Transport", false, false, false, false);
    public static FeeHeadDefinition Security { get; } = new("SECURITY", "Security Deposit", true, false, false, true);
    public static FeeHeadDefinition Repeat { get; } = new("REPEAT", "Repeat Course", false, true, false, false);

    public static IReadOnlyList<FeeHeadDefinition> All { get; } =
    [
        Tuition, Admission, Registration, Exam, Lab, Library, Hostel, Transport, Security, Repeat,
    ];
}

public static class SampleRules
{
    public static FeeRuleDefinition TuitionPerCreditHour(
        decimal rate = 9500m,
        decimal? includedHours = null,
        decimal? overloadRate = null) =>
        new(
            "rule-tuition-pch",
            PilotFeeHeads.Tuition.Code,
            FeeRuleType.PerCreditHour,
            1,
            new PerCreditHourParameters(1, rate, null, includedHours, overloadRate));

    public static FeeRuleDefinition RepeatCourse(decimal amount = 15_000m) =>
        new(
            "rule-repeat",
            PilotFeeHeads.Repeat.Code,
            FeeRuleType.PerRepeatCourse,
            1,
            new PerRepeatCourseParameters(1, amount));

    public static FeeRuleDefinition HostelConditional(decimal amount = 45_000m) =>
        new(
            "rule-hostel",
            PilotFeeHeads.Hostel.Code,
            FeeRuleType.Conditional,
            1,
            new ConditionalParameters(1, amount, "hostelResident", "true"));

    public static FeeRuleDefinition AdmissionOneTime(decimal amount = 25_000m) =>
        new(
            "rule-admission",
            PilotFeeHeads.Admission.Code,
            FeeRuleType.OneTime,
            1,
            new OneTimeParameters(1, amount, FirstTermOnly: true));

    public static FeeRuleDefinition LibraryFlat(decimal amount = 2_000m) =>
        new(
            "rule-library",
            PilotFeeHeads.Library.Code,
            FeeRuleType.FlatPerTerm,
            1,
            new FlatPerTermParameters(1, amount));
}

public static class SampleDiscounts
{
    public static DiscountRuleDefinition MeritPercent(decimal percent = 25m) =>
        new(
            "disc-merit",
            "Merit scholarship",
            DiscountCategory.Merit,
            DiscountKind.Percentage,
            percent,
            [PilotFeeHeads.Tuition.Code]);

    public static DiscountRuleDefinition SiblingPercent(decimal percent = 10m) =>
        new(
            "disc-sibling",
            "Sibling concession",
            DiscountCategory.Sibling,
            DiscountKind.Percentage,
            percent,
            [PilotFeeHeads.Tuition.Code]);
}
