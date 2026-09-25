using FeePlatform.Modules.FeeEngine.Api;
using FeePlatform.Modules.FeeEngine.Money;
using FeePlatform.Modules.FeeEngine.Models;
using FeePlatform.Modules.FeeEngine.Models.Discounts;

namespace FeePlatform.Modules.FeeEngine.Engine;

/// <summary>Pure discount stacking: order + optional max caps (plan §7.3).</summary>
public static class DiscountStacking
{
    public static IReadOnlyList<StatementLine> Apply(
        IReadOnlyList<StatementLine> chargeLines,
        IReadOnlyDictionary<string, FeeHeadDefinition> heads,
        IReadOnlyList<DiscountRuleDefinition> discounts,
        DiscountStackingPolicy policy)
    {
        foreach (var d in discounts)
            d.Validate();

        var eligibleByHead = chargeLines
            .Where(l => l.Kind == StatementLineKind.Charge)
            .Where(l => heads.TryGetValue(l.FeeHeadCode, out var h) && h.Discountable)
            .GroupBy(l => l.FeeHeadCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Amount),
                StringComparer.OrdinalIgnoreCase);

        if (eligibleByHead.Count == 0 || discounts.Count == 0)
            return Array.Empty<StatementLine>();

        var remainingEligible = new Dictionary<string, decimal>(eligibleByHead, StringComparer.OrdinalIgnoreCase);
        var eligiblePool = remainingEligible.Values.Sum();
        var ordered = OrderDiscounts(discounts, policy.Order);
        var result = new List<StatementLine>();
        decimal totalDiscountAbs = 0;
        var maxAmount = policy.MaxTotalDiscountAmount;
        if (policy.MaxTotalDiscountPercentOfEligible is { } maxPct)
        {
            var pctCap = MoneyMath.RoundHalfUp(eligiblePool * maxPct / 100m);
            maxAmount = maxAmount is null ? pctCap : Math.Min(maxAmount.Value, pctCap);
        }

        foreach (var discount in ordered)
        {
            if (!policy.AllowStacking && result.Count > 0)
                break;

            var targets = discount.TargetFeeHeadCodes
                .Where(remainingEligible.ContainsKey)
                .ToList();
            if (targets.Count == 0)
                continue;

            var baseForDiscount = targets.Sum(t => remainingEligible[t]);
            if (baseForDiscount <= 0)
                continue;

            decimal raw;
            if (discount.Kind == DiscountKind.Percentage)
                raw = MoneyMath.RoundHalfUp(baseForDiscount * discount.Value / 100m);
            else
                raw = MoneyMath.RoundHalfUp(Math.Min(discount.Value, baseForDiscount));

            if (raw <= 0)
                continue;

            var wasCapped = false;
            if (maxAmount is { } cap)
            {
                var room = cap - totalDiscountAbs;
                if (room <= 0)
                    break;
                if (raw > room)
                {
                    raw = MoneyMath.RoundHalfUp(room);
                    wasCapped = true;
                }
            }

            string trace;
            if (discount.Kind == DiscountKind.Percentage)
            {
                var ofWhat = targets.Count == 1
                    ? heads[targets[0]].Name.ToLowerInvariant()
                    : "eligible heads";
                trace = TraceFormatter.DiscountPercent(discount.Name, discount.Value, ofWhat, -raw);
            }
            else
            {
                trace = TraceFormatter.DiscountFixed(discount.Name, -raw);
            }

            if (wasCapped)
                trace += " (policy max applied)";

            // Allocate across target heads proportionally for remaining-eligible tracking.
            AllocateAcrossHeads(remainingEligible, targets, raw);

            var primaryHead = targets[0];
            result.Add(new StatementLine(
                FeeHeadCode: primaryHead,
                Description: discount.Name,
                Amount: MoneyMath.RoundHalfUp(-raw),
                Trace: trace,
                Kind: StatementLineKind.Discount,
                DiscountId: discount.Id));

            totalDiscountAbs += raw;
            if (maxAmount is { } m && totalDiscountAbs >= m)
                break;
        }

        return result;
    }

    private static IEnumerable<DiscountRuleDefinition> OrderDiscounts(
        IReadOnlyList<DiscountRuleDefinition> discounts,
        IReadOnlyList<DiscountCategory> order)
    {
        var rank = order
            .Select((c, i) => (c, i))
            .ToDictionary(x => x.c, x => x.i);

        return discounts
            .OrderBy(d => rank.TryGetValue(d.Category, out var r) ? r : int.MaxValue)
            .ThenBy(d => d.Id, StringComparer.Ordinal);
    }

    private static void AllocateAcrossHeads(
        Dictionary<string, decimal> remaining,
        IReadOnlyList<string> targets,
        decimal discountAbs)
    {
        var pool = targets.Sum(t => remaining[t]);
        if (pool <= 0)
            return;

        decimal allocated = 0;
        for (var i = 0; i < targets.Count; i++)
        {
            var head = targets[i];
            decimal take;
            if (i == targets.Count - 1)
                take = MoneyMath.RoundHalfUp(discountAbs - allocated);
            else
            {
                take = MoneyMath.RoundHalfUp(discountAbs * (remaining[head] / pool));
                allocated += take;
            }

            remaining[head] = MoneyMath.RoundHalfUp(remaining[head] - take);
        }
    }
}
