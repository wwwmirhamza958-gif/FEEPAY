using System.Text.Json;
using System.Text.Json.Serialization;
using FeePlatform.Modules.FeeEngine.Money;

namespace FeePlatform.Modules.FeeEngine.Models.Rules;

/// <summary>Marker for versioned, validated rule parameter payloads.</summary>
public interface IRuleParameters
{
    int SchemaVersion { get; }
}

public sealed record PerCreditHourParameters(
    int SchemaVersion,
    decimal DefaultRate,
    IReadOnlyDictionary<string, decimal>? CategoryRates = null,
    decimal? IncludedHours = null,
    decimal? OverloadRate = null) : IRuleParameters
{
    public const int CurrentSchemaVersion = 1;

    public static PerCreditHourParameters FromJson(JsonElement json, int schemaVersion)
    {
        RuleParameterHelpers.EnsureVersion(schemaVersion, CurrentSchemaVersion, nameof(PerCreditHourParameters));
        var defaultRate = RuleParameterHelpers.ReadMoney(json, "defaultRate");
        Dictionary<string, decimal>? categories = null;
        if (json.TryGetProperty("categoryRates", out var cat) && cat.ValueKind == JsonValueKind.Object)
        {
            categories = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in cat.EnumerateObject())
                categories[p.Name] = MoneyMath.FromWire(p.Value.GetString()!);
        }

        decimal? included = json.TryGetProperty("includedHours", out var ih) ? ih.GetDecimal() : null;
        decimal? overload = json.TryGetProperty("overloadRate", out var or)
            ? MoneyMath.FromWire(or.GetString()!)
            : null;

        return new PerCreditHourParameters(schemaVersion, defaultRate, categories, included, overload);
    }
}

public sealed record PerCourseParameters(int SchemaVersion, decimal AmountPerCourse) : IRuleParameters
{
    public const int CurrentSchemaVersion = 1;

    public static PerCourseParameters FromJson(JsonElement json, int schemaVersion)
    {
        RuleParameterHelpers.EnsureVersion(schemaVersion, CurrentSchemaVersion, nameof(PerCourseParameters));
        return new PerCourseParameters(schemaVersion, RuleParameterHelpers.ReadMoney(json, "amountPerCourse"));
    }
}

public sealed record PerRepeatCourseParameters(int SchemaVersion, decimal AmountPerCourse) : IRuleParameters
{
    public const int CurrentSchemaVersion = 1;

    public static PerRepeatCourseParameters FromJson(JsonElement json, int schemaVersion)
    {
        RuleParameterHelpers.EnsureVersion(schemaVersion, CurrentSchemaVersion, nameof(PerRepeatCourseParameters));
        return new PerRepeatCourseParameters(schemaVersion, RuleParameterHelpers.ReadMoney(json, "amountPerCourse"));
    }
}

public sealed record FlatPerTermParameters(int SchemaVersion, decimal Amount) : IRuleParameters
{
    public const int CurrentSchemaVersion = 1;

    public static FlatPerTermParameters FromJson(JsonElement json, int schemaVersion)
    {
        RuleParameterHelpers.EnsureVersion(schemaVersion, CurrentSchemaVersion, nameof(FlatPerTermParameters));
        return new FlatPerTermParameters(schemaVersion, RuleParameterHelpers.ReadMoney(json, "amount"));
    }
}

public sealed record FlatMonthlyParameters(int SchemaVersion, decimal AmountPerMonth) : IRuleParameters
{
    public const int CurrentSchemaVersion = 1;

    public static FlatMonthlyParameters FromJson(JsonElement json, int schemaVersion)
    {
        RuleParameterHelpers.EnsureVersion(schemaVersion, CurrentSchemaVersion, nameof(FlatMonthlyParameters));
        return new FlatMonthlyParameters(schemaVersion, RuleParameterHelpers.ReadMoney(json, "amountPerMonth"));
    }
}

public sealed record OneTimeParameters(int SchemaVersion, decimal Amount, bool FirstTermOnly = true) : IRuleParameters
{
    public const int CurrentSchemaVersion = 1;

    public static OneTimeParameters FromJson(JsonElement json, int schemaVersion)
    {
        RuleParameterHelpers.EnsureVersion(schemaVersion, CurrentSchemaVersion, nameof(OneTimeParameters));
        var firstOnly = !json.TryGetProperty("firstTermOnly", out var ft) || ft.GetBoolean();
        return new OneTimeParameters(schemaVersion, RuleParameterHelpers.ReadMoney(json, "amount"), firstOnly);
    }
}

public sealed record ConditionalParameters(
    int SchemaVersion,
    decimal Amount,
    string AttributeKey,
    string? ExpectedValue = null) : IRuleParameters
{
    public const int CurrentSchemaVersion = 1;

    public static ConditionalParameters FromJson(JsonElement json, int schemaVersion)
    {
        RuleParameterHelpers.EnsureVersion(schemaVersion, CurrentSchemaVersion, nameof(ConditionalParameters));
        var key = json.GetProperty("attributeKey").GetString()
                  ?? throw new ArgumentException("attributeKey required");
        string? expected = json.TryGetProperty("expectedValue", out var ev) ? ev.GetString() : null;
        return new ConditionalParameters(schemaVersion, RuleParameterHelpers.ReadMoney(json, "amount"), key, expected);
    }
}

/// <summary>Inert tax-rule slot (plan §7.4). Never produces statement lines in P1.</summary>
public sealed record TaxRuleParameters(int SchemaVersion, JsonElement? ParametersJson = null) : IRuleParameters
{
    public const int CurrentSchemaVersion = 1;
}

internal static class RuleParameterHelpers
{
    public static void EnsureVersion(int actual, int supported, string name)
    {
        if (actual < 1 || actual > supported)
            throw new ArgumentException($"{name} schema_version {actual} is not supported (max {supported}).");
    }

    public static decimal ReadMoney(JsonElement json, string property)
    {
        if (!json.TryGetProperty(property, out var el))
            throw new ArgumentException($"Missing parameter '{property}'.");
        return el.ValueKind == JsonValueKind.String
            ? MoneyMath.FromWire(el.GetString()!)
            : MoneyMath.RoundHalfUp(el.GetDecimal());
    }
}

public static class RuleParametersParser
{
    public static IRuleParameters Parse(FeeRuleType ruleType, int schemaVersion, JsonElement parameters) =>
        ruleType switch
        {
            FeeRuleType.PerCreditHour => PerCreditHourParameters.FromJson(parameters, schemaVersion),
            FeeRuleType.PerCourse => PerCourseParameters.FromJson(parameters, schemaVersion),
            FeeRuleType.PerRepeatCourse => PerRepeatCourseParameters.FromJson(parameters, schemaVersion),
            FeeRuleType.FlatPerTerm => FlatPerTermParameters.FromJson(parameters, schemaVersion),
            FeeRuleType.FlatMonthly => FlatMonthlyParameters.FromJson(parameters, schemaVersion),
            FeeRuleType.OneTime => OneTimeParameters.FromJson(parameters, schemaVersion),
            FeeRuleType.Conditional => ConditionalParameters.FromJson(parameters, schemaVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(ruleType), ruleType, null),
        };
}
