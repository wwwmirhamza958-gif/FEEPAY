using FeePlatform.Modules.FeeEngine.Api;
using FeePlatform.Modules.FeeEngine.Engine;
using FeePlatform.Modules.FeeEngine.Fixtures;
using FeePlatform.Modules.FeeEngine.Models;
using FeePlatform.Modules.FeeEngine.Models.Instalments;
using FeePlatform.Modules.FeeEngine.Models.LateFees;
using FeePlatform.Modules.FeeEngine.Models.Rules;
using EngineImpl = FeePlatform.Modules.FeeEngine.Api.FeeEngine;

namespace FeePlatform.FeeEngine.Tests;

public class RuleTypeCoverageTests
{
    private static readonly EngineImpl Engine = new();

    [Fact]
    public void All_mvp_rule_types_evaluate()
    {
        var enrolment = new EnrolmentSnapshot(
            [
                new RegisteredCourse("CS101", 3),
                new RegisteredCourse("CS102", 3, IsRepeat: true),
            ],
            IsFirstTerm: true,
            MonthCount: 4);

        var input = new FeeEngineInput(
            StudentId: "STU-PILOT-0001",
            TermCode: "FALL-2026",
            FeeHeads: PilotFeeHeads.All,
            Rules:
            [
                SampleRules.TuitionPerCreditHour(),
                new FeeRuleDefinition("r-course", "LAB", FeeRuleType.PerCourse, 1, new PerCourseParameters(1, 1_000m)),
                SampleRules.RepeatCourse(),
                SampleRules.LibraryFlat(),
                new FeeRuleDefinition("r-month", "TRANSPORT", FeeRuleType.FlatMonthly, 1, new FlatMonthlyParameters(1, 5_000m)),
                SampleRules.AdmissionOneTime(),
                SampleRules.HostelConditional(),
            ],
            Enrolment: enrolment,
            Attributes: new StudentAttributes(HostelResident: true));

        var result = Engine.Calculate(input);
        Assert.Contains(result.Lines, l => l.RuleId == "rule-tuition-pch");
        Assert.Contains(result.Lines, l => l.RuleId == "r-course");
        Assert.Contains(result.Lines, l => l.RuleId == "rule-repeat");
        Assert.Contains(result.Lines, l => l.RuleId == "rule-library");
        Assert.Contains(result.Lines, l => l.RuleId == "r-month");
        Assert.Contains(result.Lines, l => l.RuleId == "rule-admission");
        Assert.Contains(result.Lines, l => l.RuleId == "rule-hostel");
        Assert.Empty(result.TaxLines);
    }

    [Fact]
    public void Instalment_splitter_and_late_fee_helpers_are_pure()
    {
        var parts = InstalmentSplitter.Split(100_000m, new InstalmentPlanDefinition("p", [40m, 30m, 30m]));
        Assert.Equal(3, parts.Count);
        Assert.Equal(100_000m, parts.Sum(p => p.Amount));

        var late = LateFeeCalculator.Calculate(new LateFeeRequest(LateFeeMode.PerWeek, 500m, 3, Cap: 1_000m));
        Assert.Equal(1_000m, late.Amount);
        Assert.Equal(StatementLineKind.LateFee, late.Kind);
    }
}
