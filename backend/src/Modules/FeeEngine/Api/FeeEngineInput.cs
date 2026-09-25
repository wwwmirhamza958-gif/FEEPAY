using FeePlatform.Modules.FeeEngine.Models;
using FeePlatform.Modules.FeeEngine.Models.Adjustments;
using FeePlatform.Modules.FeeEngine.Models.Discounts;
using FeePlatform.Modules.FeeEngine.Models.Instalments;
using FeePlatform.Modules.FeeEngine.Models.LateFees;
using FeePlatform.Modules.FeeEngine.Models.Rules;

namespace FeePlatform.Modules.FeeEngine.Api;

/// <summary>
/// Pure calculation input. Preview and issue use the same shape; the engine has no side effects.
/// </summary>
public sealed record FeeEngineInput(
    string StudentId,
    string TermCode,
    IReadOnlyList<FeeHeadDefinition> FeeHeads,
    IReadOnlyList<FeeRuleDefinition> Rules,
    EnrolmentSnapshot Enrolment,
    StudentAttributes Attributes,
    IReadOnlyList<DiscountRuleDefinition>? Discounts = null,
    DiscountStackingPolicy? StackingPolicy = null,
    IReadOnlyList<AdjustmentInput>? Adjustments = null,
    LateFeeRequest? LateFee = null,
    InstalmentPlanDefinition? InstalmentPlan = null,
    StatementSnapshot? PreviousTermStatement = null,
    IReadOnlyList<TaxRuleDefinition>? TaxRules = null,
    bool IsPreview = true);
