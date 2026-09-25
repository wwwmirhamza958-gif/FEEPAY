using FeePlatform.Modules.FeeEngine.Money;
using FeePlatform.Modules.FeeEngine.Models;
using FeePlatform.Modules.FeeEngine.Models.Rules;

namespace FeePlatform.Modules.FeeEngine.Engine;

internal static class TraceFormatter
{
    public static string Multiply(decimal quantity, string quantityLabel, decimal rate, decimal total) =>
        $"{FormatQty(quantity)} {quantityLabel} × {MoneyMath.FormatPkr(rate)} = {MoneyMath.FormatPkr(total)}";

    public static string Flat(string label, decimal amount) =>
        $"{label} = {MoneyMath.FormatPkr(amount)}";

    public static string DiscountPercent(string name, decimal percent, string ofWhat, decimal amount) =>
        $"{name} {TrimPercent(percent)}% of {ofWhat} = {MoneyMath.FormatPkr(amount)}";

    public static string DiscountFixed(string name, decimal amount) =>
        $"{name} = {MoneyMath.FormatPkr(amount)}";

    public static string LateFeeFixed(decimal amount) =>
        $"Late fee (fixed) = {MoneyMath.FormatPkr(amount)}";

    public static string LateFeePerDay(int days, decimal rate, decimal amount) =>
        $"{days} day(s) × {MoneyMath.FormatPkr(rate)} = {MoneyMath.FormatPkr(amount)}";

    public static string LateFeePerWeek(int weeks, decimal rate, decimal amount) =>
        $"{weeks} week(s) × {MoneyMath.FormatPkr(rate)} = {MoneyMath.FormatPkr(amount)}";

    public static string Instalment(int sequence, decimal percent, decimal amount) =>
        $"Instalment {sequence}: {TrimPercent(percent)}% = {MoneyMath.FormatPkr(amount)}";

    private static string FormatQty(decimal q) =>
        q == decimal.Truncate(q)
            ? ((long)q).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : q.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static string TrimPercent(decimal p) =>
        p == decimal.Truncate(p)
            ? ((long)p).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : p.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}

internal readonly record struct ChargeComputation(
    decimal Amount,
    string Trace,
    bool Applied);

internal static class RuleEvaluator
{
    public static ChargeComputation Evaluate(
        FeeRuleDefinition rule,
        FeeHeadDefinition head,
        EnrolmentSnapshot enrolment,
        StudentAttributes attributes)
    {
        return rule.RuleType switch
        {
            FeeRuleType.PerCreditHour => EvalPerCreditHour((PerCreditHourParameters)rule.Parameters, enrolment),
            FeeRuleType.PerCourse => EvalPerCourse((PerCourseParameters)rule.Parameters, enrolment),
            FeeRuleType.PerRepeatCourse => EvalPerRepeat((PerRepeatCourseParameters)rule.Parameters, enrolment),
            FeeRuleType.FlatPerTerm => EvalFlatPerTerm((FlatPerTermParameters)rule.Parameters),
            FeeRuleType.FlatMonthly => EvalFlatMonthly((FlatMonthlyParameters)rule.Parameters, enrolment),
            FeeRuleType.OneTime => EvalOneTime((OneTimeParameters)rule.Parameters, head, enrolment),
            FeeRuleType.Conditional => EvalConditional((ConditionalParameters)rule.Parameters, attributes),
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule.RuleType, null),
        };
    }

    private static ChargeComputation EvalPerCreditHour(PerCreditHourParameters p, EnrolmentSnapshot enrolment)
    {
        if (p.CategoryRates is { Count: > 0 })
        {
            decimal total = 0;
            var parts = new List<string>();
            foreach (var course in enrolment.Courses.Where(c => !c.IsRepeat))
            {
                var rate = course.Category is not null
                           && p.CategoryRates.TryGetValue(course.Category, out var catRate)
                    ? catRate
                    : p.DefaultRate;
                var line = MoneyMath.RoundHalfUp(course.CreditHours * rate);
                total += line;
                parts.Add(TraceFormatter.Multiply(course.CreditHours, $"cr ({course.CourseCode})", rate, line));
            }

            total = MoneyMath.RoundHalfUp(total);
            var trace = parts.Count == 0
                ? TraceFormatter.Multiply(0, "credit hours", p.DefaultRate, 0)
                : string.Join("; ", parts);
            return new ChargeComputation(total, trace, total != 0 || enrolment.Courses.Any(c => !c.IsRepeat));
        }

        var hours = enrolment.CreditHours;
        if (p.IncludedHours is { } included && p.OverloadRate is { } overloadRate && hours > included)
        {
            var baseAmt = MoneyMath.RoundHalfUp(included * p.DefaultRate);
            var extraHours = hours - included;
            var extraAmt = MoneyMath.RoundHalfUp(extraHours * overloadRate);
            var sum = MoneyMath.RoundHalfUp(baseAmt + extraAmt);
            var trace =
                $"{TraceFormatter.Multiply(included, "credit hours", p.DefaultRate, baseAmt)}; "
                + $"overload {TraceFormatter.Multiply(extraHours, "extra hours", overloadRate, extraAmt)}";
            return new ChargeComputation(sum, trace, true);
        }

        var amount = MoneyMath.RoundHalfUp(hours * p.DefaultRate);
        return new ChargeComputation(
            amount,
            TraceFormatter.Multiply(hours, "credit hours", p.DefaultRate, amount),
            true);
    }

    private static ChargeComputation EvalPerCourse(PerCourseParameters p, EnrolmentSnapshot enrolment)
    {
        var count = enrolment.NonRepeatCourseCount;
        var amount = MoneyMath.RoundHalfUp(count * p.AmountPerCourse);
        return new ChargeComputation(
            amount,
            TraceFormatter.Multiply(count, "course(s)", p.AmountPerCourse, amount),
            count > 0);
    }

    private static ChargeComputation EvalPerRepeat(PerRepeatCourseParameters p, EnrolmentSnapshot enrolment)
    {
        var count = enrolment.RepeatCourseCount;
        if (count == 0)
            return new ChargeComputation(0, TraceFormatter.Multiply(0, "repeat course(s)", p.AmountPerCourse, 0), false);

        var amount = MoneyMath.RoundHalfUp(count * p.AmountPerCourse);
        return new ChargeComputation(
            amount,
            TraceFormatter.Multiply(count, "repeat course(s)", p.AmountPerCourse, amount),
            true);
    }

    private static ChargeComputation EvalFlatPerTerm(FlatPerTermParameters p)
    {
        var amount = MoneyMath.RoundHalfUp(p.Amount);
        return new ChargeComputation(amount, TraceFormatter.Flat("Flat per term", amount), true);
    }

    private static ChargeComputation EvalFlatMonthly(FlatMonthlyParameters p, EnrolmentSnapshot enrolment)
    {
        var months = enrolment.MonthCount <= 0 ? 1 : enrolment.MonthCount;
        var amount = MoneyMath.RoundHalfUp(months * p.AmountPerMonth);
        return new ChargeComputation(
            amount,
            TraceFormatter.Multiply(months, "month(s)", p.AmountPerMonth, amount),
            true);
    }

    private static ChargeComputation EvalOneTime(
        OneTimeParameters p,
        FeeHeadDefinition head,
        EnrolmentSnapshot enrolment)
    {
        if (enrolment.WasChargedOneTime(head.Code))
            return new ChargeComputation(0, "One-time head already charged", false);

        if (p.FirstTermOnly && !enrolment.IsFirstTerm)
            return new ChargeComputation(0, "One-time first-term-only; not first term", false);

        var amount = MoneyMath.RoundHalfUp(p.Amount);
        return new ChargeComputation(amount, TraceFormatter.Flat("One-time", amount), true);
    }

    private static ChargeComputation EvalConditional(ConditionalParameters p, StudentAttributes attributes)
    {
        if (!attributes.Matches(p.AttributeKey, p.ExpectedValue))
            return new ChargeComputation(0, $"Conditional '{p.AttributeKey}' not matched", false);

        var amount = MoneyMath.RoundHalfUp(p.Amount);
        return new ChargeComputation(
            amount,
            TraceFormatter.Flat($"Conditional ({p.AttributeKey})", amount),
            true);
    }
}
