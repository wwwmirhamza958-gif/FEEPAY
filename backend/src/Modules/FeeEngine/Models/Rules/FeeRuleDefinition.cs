namespace FeePlatform.Modules.FeeEngine.Models.Rules;

/// <summary>
/// A schedule rule attached to a fee head. Parameters are strongly typed and schema_version-aware.
/// </summary>
public sealed record FeeRuleDefinition(
    string Id,
    string FeeHeadCode,
    FeeRuleType RuleType,
    int SchemaVersion,
    IRuleParameters Parameters)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentException("Rule Id is required.");
        if (string.IsNullOrWhiteSpace(FeeHeadCode))
            throw new ArgumentException("FeeHeadCode is required.");
        if (SchemaVersion < 1)
            throw new ArgumentException("schema_version must be >= 1.");
        if (Parameters.SchemaVersion != SchemaVersion)
            throw new ArgumentException(
                $"Rule {Id}: Parameters.SchemaVersion ({Parameters.SchemaVersion}) must match SchemaVersion ({SchemaVersion}).");
    }
}

/// <summary>
/// Inert tax rule slot — present so later phases are configuration, not surgery (plan §7.4).
/// The engine never evaluates these into statement lines in P1.
/// </summary>
public sealed record TaxRuleDefinition(
    string Id,
    int SchemaVersion,
    TaxRuleParameters Parameters);
