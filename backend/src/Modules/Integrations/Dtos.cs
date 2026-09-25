namespace FeePlatform.Modules.Integrations;

public sealed class ImportDomainException(string code, string message, string messageKey) : Exception(message)
{
    public string Code { get; } = code;
    public string MessageKey { get; } = messageKey;
}

public sealed record ImportTemplateDto(
    string Entity,
    IReadOnlyList<string> RequiredColumns,
    IReadOnlyList<string> OptionalColumns,
    string SampleCsv);

public sealed record ImportJobDto(
    Guid JobId,
    string Entity,
    string Status,
    string OriginalFileName,
    int CreatedCount,
    int UpdatedCount,
    int SkippedCount,
    int ErrorCount,
    IReadOnlyDictionary<string, string>? ColumnMapping);

public sealed record ImportDryRunResultDto(
    Guid JobId,
    string Status,
    int WouldCreate,
    int WouldUpdate,
    int WouldSkip,
    int ErrorCount,
    IReadOnlyList<ImportRowErrorDto> Errors);

public sealed record ImportRowErrorDto(int RowNumber, string Field, string Code, string Message);
public sealed record ImportCommitResultDto(
    Guid JobId,
    string Status,
    int CreatedCount,
    int UpdatedCount,
    int SkippedCount,
    int ErrorCount);
public sealed record SetColumnMappingRequest(IReadOnlyDictionary<string, string> Mapping);
