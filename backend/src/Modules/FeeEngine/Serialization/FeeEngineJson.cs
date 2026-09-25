using System.Text.Json;
using System.Text.Json.Serialization;
using FeePlatform.Modules.FeeEngine.Api;
using FeePlatform.Modules.FeeEngine.Money;

namespace FeePlatform.Modules.FeeEngine.Serialization;

/// <summary>Golden-fixture / wire serialization for FeeEngineResult (CONTRACT §1.4 amounts).</summary>
public static class FeeEngineJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string SerializeResult(FeeEngineResult result)
    {
        var dto = ToDto(result);
        return JsonSerializer.Serialize(dto, Options);
    }

    public static FeeEngineResultDto ToDto(FeeEngineResult result) => new(
        GrossTotal: MoneyMath.ToWire(result.GrossTotal),
        DiscountTotal: MoneyMath.ToWire(result.DiscountTotal),
        NetTotal: MoneyMath.ToWire(result.NetTotal),
        IsPreview: result.IsPreview,
        TaxLinesCount: result.TaxLines.Count,
        Lines: result.Lines.Select(l => new StatementLineDto(
            l.FeeHeadCode,
            l.Description,
            MoneyMath.ToWire(l.Amount),
            l.Trace,
            l.Kind.ToString())).ToList(),
        Instalments: result.Instalments?.Select(i => new InstalmentPartDto(
            i.Sequence,
            MoneyMath.ToWire(i.Amount),
            i.PercentShare,
            i.DueDate?.ToString("yyyy-MM-dd"),
            i.Trace)).ToList(),
        TermDiff: result.TermDiff is null
            ? null
            : new TermDiffDto(
                MoneyMath.ToWire(result.TermDiff.PreviousTotal),
                MoneyMath.ToWire(result.TermDiff.CurrentTotal),
                MoneyMath.ToWire(result.TermDiff.AbsoluteChange),
                result.TermDiff.TotalPercentChange,
                result.TermDiff.PerHead.Select(h => new HeadDiffDto(
                    h.FeeHeadCode,
                    MoneyMath.ToWire(h.PreviousAmount),
                    MoneyMath.ToWire(h.CurrentAmount),
                    MoneyMath.ToWire(h.AbsoluteChange),
                    h.PercentChange)).ToList()));

    public static FeeEngineResultDto ParseExpected(string json) =>
        JsonSerializer.Deserialize<FeeEngineResultDto>(json, Options)
        ?? throw new InvalidOperationException("Expected fixture JSON deserialized to null.");
}

public sealed record FeeEngineResultDto(
    string GrossTotal,
    string DiscountTotal,
    string NetTotal,
    bool IsPreview,
    int TaxLinesCount,
    IReadOnlyList<StatementLineDto> Lines,
    IReadOnlyList<InstalmentPartDto>? Instalments = null,
    TermDiffDto? TermDiff = null);

public sealed record StatementLineDto(
    string FeeHeadCode,
    string Description,
    string Amount,
    string Trace,
    string Kind);

public sealed record InstalmentPartDto(
    int Sequence,
    string Amount,
    decimal PercentShare,
    string? DueDate,
    string Trace);

public sealed record TermDiffDto(
    string PreviousTotal,
    string CurrentTotal,
    string AbsoluteChange,
    decimal? TotalPercentChange,
    IReadOnlyList<HeadDiffDto> PerHead);

public sealed record HeadDiffDto(
    string FeeHeadCode,
    string PreviousAmount,
    string CurrentAmount,
    string AbsoluteChange,
    decimal? PercentChange);
