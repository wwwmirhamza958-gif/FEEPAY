namespace FeePlatform.Infrastructure.Outbox;

/// <summary>Transactional outbox row — CONTRACT §4 names via EventName.</summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    /// <summary>Domain event name, e.g. InvoiceIssued (CONTRACT §4).</summary>
    public string EventName { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public string? LastError { get; set; }
    public int AttemptCount { get; set; }
}

public interface IOutboxDispatcher
{
    Task DispatchPendingAsync(CancellationToken cancellationToken = default);
}

/// <summary>Canonical domain event names — CONTRACT §4.</summary>
public static class DomainEvents
{
    public const string InvoiceIssued = "InvoiceIssued";
    public const string InvoiceSuperseded = "InvoiceSuperseded";
    public const string PaymentAttemptCreated = "PaymentAttemptCreated";
    public const string PaymentSucceeded = "PaymentSucceeded";
    public const string PaymentFailed = "PaymentFailed";
    public const string ClearanceChanged = "ClearanceChanged";
    public const string ImportJobCompleted = "ImportJobCompleted";
    public const string OfflinePaymentRecorded = "OfflinePaymentRecorded";
    public const string OverpaymentCredited = "OverpaymentCredited";
}
