using FeePlatform.Modules.FeeEngine.Api;
using FeePlatform.Modules.FeeEngine.Money;
using FeePlatform.Modules.FeeEngine.Models;
using FeePlatform.Modules.FeeEngine.Models.Instalments;
using FeePlatform.Modules.FeeEngine.Models.LateFees;

namespace FeePlatform.Modules.FeeEngine.Engine;

/// <summary>Instalment split math — round per part, remainder on last (CONTRACT §1.2).</summary>
public static class InstalmentSplitter
{
    public static IReadOnlyList<InstalmentPart> Split(decimal netTotal, InstalmentPlanDefinition plan)
    {
        plan.Validate();
        var total = MoneyMath.RoundHalfUp(netTotal);
        if (total < 0)
            throw new ArgumentOutOfRangeException(nameof(netTotal), "Cannot split a negative total.");

        var parts = new List<InstalmentPart>(plan.PercentShares.Count);
        decimal allocated = 0;
        for (var i = 0; i < plan.PercentShares.Count; i++)
        {
            var pct = plan.PercentShares[i];
            decimal amount;
            if (i == plan.PercentShares.Count - 1)
                amount = MoneyMath.RoundHalfUp(total - allocated);
            else
            {
                amount = MoneyMath.RoundHalfUp(total * pct / 100m);
                allocated += amount;
            }

            DateOnly? due = plan.DueDates?[i];
            parts.Add(new InstalmentPart(
                Sequence: i + 1,
                Amount: amount,
                PercentShare: pct,
                DueDate: due,
                Trace: TraceFormatter.Instalment(i + 1, pct, amount)));
        }

        return parts;
    }
}

/// <summary>Late-fee calculation helper only (plan §7.5) — no scheduling/job.</summary>
public static class LateFeeCalculator
{
    public static StatementLine Calculate(LateFeeRequest request)
    {
        if (request.SchemaVersion < 1)
            throw new ArgumentException("Late fee schema_version must be >= 1.");
        if (request.Units < 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Units must be >= 0.");

        decimal raw = request.Mode switch
        {
            LateFeeMode.Fixed => request.Rate,
            LateFeeMode.PerDay => request.Rate * request.Units,
            LateFeeMode.PerWeek => request.Rate * request.Units,
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Mode, null),
        };

        var amount = MoneyMath.RoundHalfUp(raw);
        if (request.Cap is { } cap)
            amount = MoneyMath.RoundHalfUp(Math.Min(amount, cap));

        var trace = request.Mode switch
        {
            LateFeeMode.Fixed => TraceFormatter.LateFeeFixed(amount),
            LateFeeMode.PerDay => TraceFormatter.LateFeePerDay(request.Units, request.Rate, amount),
            LateFeeMode.PerWeek => TraceFormatter.LateFeePerWeek(request.Units, request.Rate, amount),
            _ => TraceFormatter.Flat("Late fee", amount),
        };

        return new StatementLine(
            FeeHeadCode: request.FeeHeadCode,
            Description: request.Description,
            Amount: amount,
            Trace: trace,
            Kind: StatementLineKind.LateFee);
    }
}

public static class TermDiffCalculator
{
    public static TermDiffResult Diff(
        StatementSnapshot previous,
        IReadOnlyList<StatementLine> currentLines)
    {
        var currentByHead = currentLines
            .GroupBy(l => l.FeeHeadCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => MoneyMath.RoundHalfUp(g.Sum(x => x.Amount)), StringComparer.OrdinalIgnoreCase);

        var previousByHead = previous.Heads
            .GroupBy(h => h.FeeHeadCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => MoneyMath.RoundHalfUp(g.Sum(x => x.Amount)), StringComparer.OrdinalIgnoreCase);

        var keys = previousByHead.Keys
            .Union(currentByHead.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var perHead = new List<HeadDiff>(keys.Count);
        foreach (var key in keys)
        {
            previousByHead.TryGetValue(key, out var prev);
            currentByHead.TryGetValue(key, out var curr);
            var abs = MoneyMath.RoundHalfUp(curr - prev);
            decimal? pct = prev == 0
                ? (curr == 0 ? 0 : null)
                : MoneyMath.RoundHalfUp(abs / prev * 100m);
            perHead.Add(new HeadDiff(key, prev, curr, abs, pct));
        }

        var prevTotal = MoneyMath.RoundHalfUp(previousByHead.Values.Sum());
        var currTotal = MoneyMath.RoundHalfUp(currentByHead.Values.Sum());
        var totalAbs = MoneyMath.RoundHalfUp(currTotal - prevTotal);
        decimal? totalPct = prevTotal == 0
            ? (currTotal == 0 ? 0 : null)
            : MoneyMath.RoundHalfUp(totalAbs / prevTotal * 100m);

        return new TermDiffResult(perHead, prevTotal, currTotal, totalAbs, totalPct);
    }
}
