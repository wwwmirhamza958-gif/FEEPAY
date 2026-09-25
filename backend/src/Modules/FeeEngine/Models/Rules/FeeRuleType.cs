namespace FeePlatform.Modules.FeeEngine.Models.Rules;

/// <summary>MVP rule types from plan §7.2.</summary>
public enum FeeRuleType
{
    PerCreditHour,
    PerCourse,
    PerRepeatCourse,
    FlatPerTerm,
    FlatMonthly,
    OneTime,
    Conditional,
}
