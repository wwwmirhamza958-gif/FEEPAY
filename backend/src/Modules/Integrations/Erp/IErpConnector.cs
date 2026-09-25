namespace FeePlatform.Modules.Integrations.Erp;

/// <summary>
/// ERP connector framework stub (plan §8.4). One interface; no real pilot ERP yet.
/// </summary>
public interface IErpConnector
{
    string Code { get; }
    Task<ErpPullResult> PullAsync(string entity, DateTimeOffset? since, CancellationToken ct = default);
    Task PushAsync(ErpPushEvent pushEvent, CancellationToken ct = default);
}

public sealed record ErpPullResult(
    int CreatedCount,
    int UpdatedCount,
    int ErrorCount,
    IReadOnlyList<string> Errors);

public sealed record ErpPushEvent(
    string EventName,
    Guid TenantId,
    string PayloadJson,
    DateTimeOffset OccurredAtUtc);

/// <summary>No-op connector registered so DI and portal can list the framework.</summary>
public sealed class StubErpConnector : IErpConnector
{
    public string Code => "stub-erp";

    public Task<ErpPullResult> PullAsync(string entity, DateTimeOffset? since, CancellationToken ct = default) =>
        Task.FromResult(new ErpPullResult(0, 0, 0, ["Stub connector — no pilot ERP configured."]));

    public Task PushAsync(ErpPushEvent pushEvent, CancellationToken ct = default) =>
        Task.CompletedTask;
}
