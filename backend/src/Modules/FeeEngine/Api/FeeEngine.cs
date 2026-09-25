using FeePlatform.Modules.FeeEngine.Engine;
using FeePlatform.Modules.FeeEngine.Money;
using FeePlatform.Modules.FeeEngine.Models;
using FeePlatform.Modules.FeeEngine.Models.Discounts;

namespace FeePlatform.Modules.FeeEngine.Api;

/// <summary>
/// Pure fee calculation library (plan §7). No database, network, or side effects.
/// Preview and issue share this path — Billing decides whether to persist.
/// </summary>
public sealed class FeeEngine : IFeeEngine
{
    public FeeEngineResult Calculate(FeeEngineInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.StudentId))
            throw new ArgumentException("StudentId is required.", nameof(input));
        if (string.IsNullOrWhiteSpace(input.TermCode))
            throw new ArgumentException("TermCode is required.", nameof(input));

        // Tax rules are accepted but never emit lines (plan §7.4 / CONTRACT §8.2).
        _ = input.TaxRules;

        var heads = input.FeeHeads.ToDictionary(h => h.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var rule in input.Rules)
            rule.Validate();

        var lines = new List<StatementLine>();

        foreach (var rule in input.Rules)
        {
            if (!heads.TryGetValue(rule.FeeHeadCode, out var head))
                throw new InvalidOperationException(
                    $"Rule '{rule.Id}' references unknown fee head '{rule.FeeHeadCode}'.");

            var computation = RuleEvaluator.Evaluate(rule, head, input.Enrolment, input.Attributes);
            if (!computation.Applied)
                continue;

            lines.Add(new StatementLine(
                FeeHeadCode: head.Code,
                Description: head.Name,
                Amount: computation.Amount,
                Trace: computation.Trace,
                Kind: StatementLineKind.Charge,
                RuleId: rule.Id));
        }

        var discounts = input.Discounts ?? Array.Empty<DiscountRuleDefinition>();
        var policy = input.StackingPolicy ?? DiscountStackingPolicy.Default;
        var discountLines = DiscountStacking.Apply(lines, heads, discounts, policy);
        lines.AddRange(discountLines);

        if (input.Adjustments is { Count: > 0 })
        {
            foreach (var adj in input.Adjustments)
            {
                var amount = MoneyMath.RoundHalfUp(adj.Amount);
                var trace = adj.Trace
                            ?? $"{adj.Description} = {MoneyMath.FormatPkr(amount)}";
                lines.Add(new StatementLine(
                    FeeHeadCode: adj.FeeHeadCode,
                    Description: adj.Description,
                    Amount: amount,
                    Trace: trace,
                    Kind: StatementLineKind.Adjustment));
            }
        }

        if (input.LateFee is not null)
            lines.Add(LateFeeCalculator.Calculate(input.LateFee));

        var gross = MoneyMath.RoundHalfUp(
            lines.Where(l => l.Kind == StatementLineKind.Charge).Sum(l => l.Amount));
        var discountTotal = MoneyMath.RoundHalfUp(
            lines.Where(l => l.Kind == StatementLineKind.Discount).Sum(l => l.Amount));
        var net = MoneyMath.RoundHalfUp(lines.Sum(l => l.Amount));

        IReadOnlyList<InstalmentPart>? instalments = null;
        if (input.InstalmentPlan is not null)
            instalments = InstalmentSplitter.Split(net, input.InstalmentPlan);

        TermDiffResult? termDiff = null;
        if (input.PreviousTermStatement is not null)
            termDiff = TermDiffCalculator.Diff(input.PreviousTermStatement, lines);

        return new FeeEngineResult(
            Lines: lines,
            GrossTotal: gross,
            DiscountTotal: discountTotal,
            NetTotal: net,
            Instalments: instalments,
            TermDiff: termDiff,
            IsPreview: input.IsPreview);
    }
}
