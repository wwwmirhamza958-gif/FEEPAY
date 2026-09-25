namespace FeePlatform.Modules.FeeEngine.Models;

/// <summary>Student attributes evaluated by Conditional / OneTime rules.</summary>
public sealed record StudentAttributes(
    bool HostelResident = false,
    string? TransportRoute = null,
    bool EveningProgram = false,
    IReadOnlyDictionary<string, string>? Custom = null)
{
    public static StudentAttributes Empty { get; } = new();

    public bool Matches(string attributeKey, string? expectedValue)
    {
        return attributeKey switch
        {
            "hostelResident" or "HostelResident" =>
                expectedValue is null
                    ? HostelResident
                    : bool.TryParse(expectedValue, out var hb) && hb == HostelResident,
            "eveningProgram" or "EveningProgram" =>
                expectedValue is null
                    ? EveningProgram
                    : bool.TryParse(expectedValue, out var eb) && eb == EveningProgram,
            "transportRoute" or "TransportRoute" =>
                expectedValue is null
                    ? !string.IsNullOrEmpty(TransportRoute)
                    : string.Equals(TransportRoute, expectedValue, StringComparison.OrdinalIgnoreCase),
            _ => Custom is not null
                 && Custom.TryGetValue(attributeKey, out var actual)
                 && (expectedValue is null || string.Equals(actual, expectedValue, StringComparison.OrdinalIgnoreCase)),
        };
    }
}
