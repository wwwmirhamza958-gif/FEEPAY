using FeePlatform.Modules.FeeEngine;
using FeePlatform.Modules.FeeEngine.Api;
using FeePlatform.Modules.FeeEngine.Fixtures;
using FeePlatform.Modules.FeeEngine.Models;
using FeePlatform.Modules.FeeEngine.Models.Adjustments;
using FeePlatform.Modules.FeeEngine.Models.Discounts;
using FeePlatform.Modules.FeeEngine.Models.Instalments;
using FeePlatform.Modules.FeeEngine.Models.LateFees;
using FeePlatform.Modules.FeeEngine.Models.Rules;

namespace FeePlatform.FeeEngine.Tests;

/// <summary>Builds FeeEngineInput for each golden scenario (plan §7.7).</summary>
internal static class FixtureScenarios
{
    private static EnrolmentSnapshot FullLoad15() => new(
        [
            new RegisteredCourse("CS101", 3),
            new RegisteredCourse("CS102", 3),
            new RegisteredCourse("CS103", 3),
            new RegisteredCourse("MT101", 3),
            new RegisteredCourse("EN101", 3),
        ]);

    public static (string Name, FeeEngineInput Input) FullLoadCreditHourTuition() =>
    (
        "full-load-credit-hour-tuition",
        new FeeEngineInput(
            StudentId: "STU-PILOT-0001",
            TermCode: "FALL-2026",
            FeeHeads: PilotFeeHeads.All,
            Rules: [SampleRules.TuitionPerCreditHour()],
            Enrolment: FullLoad15(),
            Attributes: StudentAttributes.Empty)
    );

    public static (string Name, FeeEngineInput Input) OverloadExtraHours() =>
    (
        "overload-extra-hours",
        new FeeEngineInput(
            StudentId: "STU-PILOT-0001",
            TermCode: "FALL-2026",
            FeeHeads: PilotFeeHeads.All,
            Rules:
            [
                SampleRules.TuitionPerCreditHour(
                    rate: 9500m,
                    includedHours: 15m,
                    overloadRate: 12_000m),
            ],
            Enrolment: new EnrolmentSnapshot(
            [
                new RegisteredCourse("CS101", 3),
                new RegisteredCourse("CS102", 3),
                new RegisteredCourse("CS103", 3),
                new RegisteredCourse("MT101", 3),
                new RegisteredCourse("EN101", 3),
                new RegisteredCourse("CS201", 3), // 18 hours
            ]),
            Attributes: StudentAttributes.Empty)
    );

    public static (string Name, FeeEngineInput Input) RepeatCourse() =>
    (
        "repeat-course",
        new FeeEngineInput(
            StudentId: "STU-PILOT-0001",
            TermCode: "FALL-2026",
            FeeHeads: PilotFeeHeads.All,
            Rules:
            [
                SampleRules.TuitionPerCreditHour(),
                SampleRules.RepeatCourse(15_000m),
            ],
            Enrolment: new EnrolmentSnapshot(
            [
                new RegisteredCourse("CS101", 3),
                new RegisteredCourse("CS102", 3),
                new RegisteredCourse("CS103", 3),
                new RegisteredCourse("MT101", 3),
                new RegisteredCourse("EN101", 3),
                new RegisteredCourse("CS050", 3, IsRepeat: true),
            ]),
            Attributes: StudentAttributes.Empty)
    );

    public static (string Name, FeeEngineInput Input) ScholarshipStacking() =>
    (
        "scholarship-stacking-merit-sibling",
        new FeeEngineInput(
            StudentId: "STU-PILOT-0001",
            TermCode: "FALL-2026",
            FeeHeads: PilotFeeHeads.All,
            Rules: [SampleRules.TuitionPerCreditHour()],
            Enrolment: FullLoad15(),
            Attributes: StudentAttributes.Empty,
            Discounts: [SampleDiscounts.MeritPercent(25m), SampleDiscounts.SiblingPercent(10m)],
            StackingPolicy: new DiscountStackingPolicy(
                [DiscountCategory.Merit, DiscountCategory.Sibling],
                AllowStacking: true,
                MaxTotalDiscountAmount: 40_000m))
    );

    public static (string Name, FeeEngineInput Input) Instalment5050() =>
    (
        "instalment-50-50",
        new FeeEngineInput(
            StudentId: "STU-PILOT-0001",
            TermCode: "FALL-2026",
            FeeHeads: PilotFeeHeads.All,
            Rules: [SampleRules.TuitionPerCreditHour()],
            Enrolment: FullLoad15(),
            Attributes: StudentAttributes.Empty,
            InstalmentPlan: new InstalmentPlanDefinition(
                "plan-50-50",
                [50m, 50m],
                [new DateOnly(2026, 9, 1), new DateOnly(2026, 11, 1)]))
    );

    public static (string Name, FeeEngineInput Input) Instalment403030() =>
    (
        "instalment-40-30-30",
        new FeeEngineInput(
            StudentId: "STU-PILOT-0001",
            TermCode: "FALL-2026",
            FeeHeads: PilotFeeHeads.All,
            Rules: [SampleRules.TuitionPerCreditHour()],
            Enrolment: FullLoad15(),
            Attributes: StudentAttributes.Empty,
            InstalmentPlan: new InstalmentPlanDefinition(
                "plan-40-30-30",
                [40m, 30m, 30m],
                [new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 1)]))
    );

    public static (string Name, FeeEngineInput Input) LateFeeFixed() =>
    (
        "late-fee-fixed",
        new FeeEngineInput(
            StudentId: "STU-PILOT-0001",
            TermCode: "FALL-2026",
            FeeHeads: PilotFeeHeads.All,
            Rules: [SampleRules.TuitionPerCreditHour()],
            Enrolment: FullLoad15(),
            Attributes: StudentAttributes.Empty,
            LateFee: new LateFeeRequest(LateFeeMode.Fixed, Rate: 5_000m, Units: 0))
    );

    public static (string Name, FeeEngineInput Input) LateFeePerDay() =>
    (
        "late-fee-per-day",
        new FeeEngineInput(
            StudentId: "STU-PILOT-0001",
            TermCode: "FALL-2026",
            FeeHeads: PilotFeeHeads.All,
            Rules: [SampleRules.TuitionPerCreditHour()],
            Enrolment: FullLoad15(),
            Attributes: StudentAttributes.Empty,
            LateFee: new LateFeeRequest(LateFeeMode.PerDay, Rate: 200m, Units: 10, Cap: 5_000m))
    );

    public static (string Name, FeeEngineInput Input) AddDropAdjustment() =>
    (
        "add-drop-adjustment",
        new FeeEngineInput(
            StudentId: "STU-PILOT-0001",
            TermCode: "FALL-2026",
            FeeHeads: PilotFeeHeads.All,
            Rules: [SampleRules.TuitionPerCreditHour()],
            Enrolment: FullLoad15(),
            Attributes: StudentAttributes.Empty,
            Adjustments:
            [
                new AdjustmentInput(
                    PilotFeeHeads.Tuition.Code,
                    "Add/drop: dropped CS301 (3 cr)",
                    -28_500m,
                    Trace: null),
            ])
    );

    public static IEnumerable<(string Name, FeeEngineInput Input)> All()
    {
        yield return FullLoadCreditHourTuition();
        yield return OverloadExtraHours();
        yield return RepeatCourse();
        yield return ScholarshipStacking();
        yield return Instalment5050();
        yield return Instalment403030();
        yield return LateFeeFixed();
        yield return LateFeePerDay();
        yield return AddDropAdjustment();
    }
}
