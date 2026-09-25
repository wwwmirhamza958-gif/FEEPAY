namespace FeePlatform.Modules.FeeEngine.Api;

/// <summary>
/// Public entry point for Agent C (Billing). Pure: same inputs always yield the same draft.
/// </summary>
public interface IFeeEngine
{
    FeeEngineResult Calculate(FeeEngineInput input);
}
