using FeePlatform.Modules.FeeEngine.Api;
using FeePlatform.Modules.FeeEngine.Fixtures;
using FeePlatform.Modules.FeeEngine.Models;
using FeePlatform.Modules.FeeEngine.Models.Rules;
using FeePlatform.Modules.FeeEngine.Serialization;
using EngineImpl = FeePlatform.Modules.FeeEngine.Api.FeeEngine;

namespace FeePlatform.FeeEngine.Tests;

public class GoldenFixtureTests
{
    private static readonly EngineImpl Engine = new();

    public static IEnumerable<object[]> FixtureNames() =>
        FixtureScenarios.All().Select(s => new object[] { s.Name });

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void Golden_fixture_matches_committed_expected_json(string fixtureName)
    {
        var scenario = FixtureScenarios.All().Single(s => s.Name == fixtureName);
        var result = Engine.Calculate(scenario.Input);
        var actual = FeeEngineJson.ToDto(result);

        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", $"{fixtureName}.expected.json");
        Assert.True(File.Exists(path), $"Missing golden fixture: {path}");

        var expectedJson = File.ReadAllText(path);
        var expected = FeeEngineJson.ParseExpected(expectedJson);

        AssertDtoEqual(expected, actual);
    }

    [Fact]
    public void Tax_rules_emit_nothing_not_even_zero_lines()
    {
        var (name, input) = FixtureScenarios.FullLoadCreditHourTuition();
        _ = name;
        var withTax = input with
        {
            TaxRules =
            [
                new TaxRuleDefinition(
                    "tax-inert",
                    1,
                    new TaxRuleParameters(1)),
            ],
        };

        var result = Engine.Calculate(withTax);
        Assert.Empty(result.TaxLines);
        Assert.DoesNotContain(result.Lines, l => l.FeeHeadCode.Contains("TAX", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, FeeEngineJson.ToDto(result).TaxLinesCount);
    }

    [Fact]
    public void Term_over_term_diff_reports_per_head_and_total_percent()
    {
        var (_, input) = FixtureScenarios.FullLoadCreditHourTuition();
        var previous = new StatementSnapshot(
            "SPRING-2026",
            new[] { new SnapshotHeadAmount("TUITION", 130_000m) });
        var withPrev = input with { PreviousTermStatement = previous };

        var result = Engine.Calculate(withPrev);
        Assert.NotNull(result.TermDiff);
        Assert.Equal(130_000m, result.TermDiff!.PreviousTotal);
        Assert.Equal(142_500m, result.TermDiff.CurrentTotal);
        Assert.Equal(12_500m, result.TermDiff.AbsoluteChange);
        Assert.NotNull(result.TermDiff.TotalPercentChange);
        var tuition = Assert.Single(result.TermDiff.PerHead, h => h.FeeHeadCode == "TUITION");
        Assert.Equal(12_500m, tuition.AbsoluteChange);
    }

    [Fact]
    public void Preview_flag_is_echoed_and_engine_is_side_effect_free()
    {
        var (_, input) = FixtureScenarios.FullLoadCreditHourTuition();
        var a = Engine.Calculate(input with { IsPreview = true });
        var b = Engine.Calculate(input with { IsPreview = true });
        Assert.True(a.IsPreview);
        Assert.Equal(FeeEngineJson.SerializeResult(a), FeeEngineJson.SerializeResult(b));
    }

    private static void AssertDtoEqual(FeeEngineResultDto expected, FeeEngineResultDto actual)
    {
        Assert.Equal(expected.GrossTotal, actual.GrossTotal);
        Assert.Equal(expected.DiscountTotal, actual.DiscountTotal);
        Assert.Equal(expected.NetTotal, actual.NetTotal);
        Assert.Equal(expected.TaxLinesCount, actual.TaxLinesCount);
        Assert.Equal(expected.Lines.Count, actual.Lines.Count);
        for (var i = 0; i < expected.Lines.Count; i++)
        {
            Assert.Equal(expected.Lines[i].FeeHeadCode, actual.Lines[i].FeeHeadCode);
            Assert.Equal(expected.Lines[i].Description, actual.Lines[i].Description);
            Assert.Equal(expected.Lines[i].Amount, actual.Lines[i].Amount);
            Assert.Equal(expected.Lines[i].Trace, actual.Lines[i].Trace);
            Assert.Equal(expected.Lines[i].Kind, actual.Lines[i].Kind);
        }

        if (expected.Instalments is null)
            Assert.Null(actual.Instalments);
        else
        {
            Assert.NotNull(actual.Instalments);
            Assert.Equal(expected.Instalments.Count, actual.Instalments!.Count);
            for (var i = 0; i < expected.Instalments.Count; i++)
            {
                Assert.Equal(expected.Instalments[i].Sequence, actual.Instalments[i].Sequence);
                Assert.Equal(expected.Instalments[i].Amount, actual.Instalments[i].Amount);
                Assert.Equal(expected.Instalments[i].PercentShare, actual.Instalments[i].PercentShare);
                Assert.Equal(expected.Instalments[i].DueDate, actual.Instalments[i].DueDate);
                Assert.Equal(expected.Instalments[i].Trace, actual.Instalments[i].Trace);
            }
        }
    }
}

/// <summary>
/// Helper to regenerate expected JSON when intentionally changing fixtures.
/// Run: dotnet test --filter GenerateExpected -- and inspect skipped output, or call from a one-off.
/// </summary>
public class FixtureGenerator
{
    [Fact(Skip = "Manual: remove Skip to rewrite Fixtures/*.expected.json")]
    public void GenerateExpected()
    {
        var engine = new EngineImpl();
        var dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
        Directory.CreateDirectory(dir);
        foreach (var (name, input) in FixtureScenarios.All())
        {
            var json = FeeEngineJson.SerializeResult(engine.Calculate(input));
            File.WriteAllText(Path.Combine(dir, $"{name}.expected.json"), json + Environment.NewLine);
        }
    }
}
